import { CurrencyPipe, DatePipe, LowerCasePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, ElementRef, computed, effect, inject, signal, untracked, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Chart, registerables } from 'chart.js';
import flatpickr from 'flatpickr';
import type { CustomLocale } from 'flatpickr/dist/types/locale';
import { Subscription } from 'rxjs';

import { ThemeService } from '../../../core/theme/theme.service';
import { LanguageService } from '../../../core/i18n/language.service';
import { Dropdown, DropdownOption } from '../../../shared/ui/dropdown/dropdown.component';
import { portugalToday } from '../../../shared/util/portugal-calendar';
import { DashboardDaily, DashboardData, DashboardDataService } from './dashboard-data.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

Chart.register(...registerables);

type Period = '30d' | '90d' | '12m' | 'custom';
interface DateRange { readonly start: Date; readonly end: Date; }

const portugueseCalendarLocale: CustomLocale = {
  weekdays: {
    shorthand: ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'],
    longhand: ['Domingo', 'Segunda-feira', 'Terça-feira', 'Quarta-feira', 'Quinta-feira', 'Sexta-feira', 'Sábado'],
  },
  months: {
    shorthand: ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez'],
    longhand: ['Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho', 'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'],
  },
  firstDayOfWeek: 1,
  rangeSeparator: ' até ',
  time_24hr: true,
};

interface ChartSeries {
  readonly labels: readonly string[];
  readonly revenue: readonly number[];
  readonly profit: readonly number[];
}

interface SourceProfit {
  readonly name: string;
  readonly profit: number;
}

@Component({
  imports: [CurrencyPipe, DatePipe, Dropdown, LowerCasePipe, RouterLink, TranslatePipe],
  selector: 'app-dashboard',
  styleUrl: './dashboard.component.scss',
  templateUrl: './dashboard.component.html',
})
export class Dashboard {
  private readonly dashboardDataService = inject(DashboardDataService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly themeService = inject(ThemeService);
  protected readonly language = inject(LanguageService);
  private profitChart: Chart<'line'> | null = null;
  private sourceChart: Chart<'bar'> | null = null;
  private chartTheme: string | null = null;
  private chartLocale: string | null = null;
  private rangePicker: flatpickr.Instance | null = null;
  private loadSubscription: Subscription | null = null;

  protected readonly data = signal<DashboardData | null>(null);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly period = signal<Period>('30d');
  protected readonly customRange = signal<readonly [Date, Date] | null>(null);
  protected readonly openPickerOnCreate = signal(false);
  protected readonly periodOptions: readonly DropdownOption[] = [
    { value: '30d', label: 'Últimos 30 dias' },
    { value: '90d', label: 'Últimos 90 dias' },
    { value: '12m', label: 'Últimos 12 meses' },
    { value: 'custom', label: 'Personalizado' },
  ];
  protected readonly customRangeInput = viewChild<ElementRef<HTMLInputElement>>('customRangeInput');
  protected readonly profitCanvas = viewChild<ElementRef<HTMLCanvasElement>>('profitCanvas');
  protected readonly sourceCanvas = viewChild<ElementRef<HTMLCanvasElement>>('sourceCanvas');

  protected readonly periodLabel = computed(() => {
    const period = this.period();
    if (period !== 'custom') {
      return ({ '30d': 'Últimos 30 dias', '90d': 'Últimos 90 dias', '12m': 'Últimos 12 meses' } as const)[period];
    }
    const range = this.customRange();
    return range ? `${this.formatDate(range[0])} – ${this.formatDate(range[1])}` : 'Intervalo personalizado';
  });

  protected readonly summary = computed(() => {
    const data = this.data();
    return {
      revenue: data?.revenue ?? 0,
      profit: data?.profit ?? 0,
      soldCount: data?.soldCount ?? 0,
      invested: data?.invested ?? 0,
      stockCount: data?.stockCount ?? 0,
      sales: data?.sales ?? [],
      stock: data?.stock ?? [],
      sourceProfits: data?.sourceProfits ?? [],
      chart: this.buildChartSeries(data?.daily ?? [], this.period(), this.periodBounds(this.period())),
    };
  });

  constructor() {
    this.load();
    effect((onCleanup) => {
      const input = this.customRangeInput()?.nativeElement;
      if (!input) return;

      const initialRange = untracked(() => this.customRange());
      const picker = flatpickr(input, {
        mode: 'range',
        locale: this.language.isEnglish() ? 'default' : portugueseCalendarLocale,
        dateFormat: 'd/m/Y',
        maxDate: portugalToday(),
        disableMobile: true,
        defaultDate: initialRange ? [...initialRange] : undefined,
        onChange: (dates) => {
          if (dates.length !== 2) return;
          const [first, last] = dates;
          const start = first <= last ? first : last;
          const end = first <= last ? last : first;
          this.customRange.set([this.startOfDay(start), this.endOfDay(end)]);
          this.load();
        },
      });
      this.rangePicker = picker;

      if (untracked(() => this.openPickerOnCreate())) {
        untracked(() => this.openPickerOnCreate.set(false));
        queueMicrotask(() => picker.open());
      }
      onCleanup(() => {
        picker.destroy();
        if (this.rangePicker === picker) this.rangePicker = null;
      });
    });

    effect(() => {
      const profitCanvas = this.profitCanvas()?.nativeElement;
      const sourceCanvas = this.sourceCanvas()?.nativeElement;
      const summary = this.summary();
      const theme = this.themeService.theme();
      const locale = this.language.locale();
      if (!profitCanvas || !sourceCanvas || !this.data()) {
        this.profitChart?.destroy();
        this.sourceChart?.destroy();
        this.profitChart = null;
        this.sourceChart = null;
        return;
      }
      this.updateCharts(profitCanvas, sourceCanvas, summary.chart, summary.sourceProfits, theme, locale);
    });

    this.destroyRef.onDestroy(() => {
      this.loadSubscription?.unsubscribe();
      this.profitChart?.destroy();
      this.sourceChart?.destroy();
    });
  }

  protected selectPeriod(value: string): void {
    if (value !== '30d' && value !== '90d' && value !== '12m' && value !== 'custom') return;
    if (value === 'custom') {
      if (this.period() === 'custom' && this.rangePicker) {
        this.rangePicker.open();
        return;
      }
      if (!this.customRange()) {
        const recentRange = this.periodBounds('30d');
        if (recentRange) this.customRange.set([recentRange.start, recentRange.end]);
      }
      this.openPickerOnCreate.set(true);
    }
    this.period.set(value);
    this.load();
  }

  protected refresh(): void {
    this.load();
  }

  private load(): void {
    const bounds = this.periodBounds(this.period());
    if (!bounds) return;

    const toExclusive = new Date(bounds.end.getFullYear(), bounds.end.getMonth(), bounds.end.getDate() + 1);
    this.loadSubscription?.unsubscribe();
    this.loading.set(true);
    this.errorMessage.set(null);
    this.loadSubscription = this.dashboardDataService
      .load(this.dateKey(bounds.start), this.dateKey(toExclusive))
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.data.set(data);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.getErrorMessage(error));
        this.loading.set(false);
      },
    });
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse && error.status === 0) {
      return 'Não foi possível ligar ao servidor. Confirma a ligação e tenta novamente.';
    }
    if (error instanceof HttpErrorResponse && error.status === 401) {
      return 'A sessão expirou. Inicia sessão novamente para consultar o dashboard.';
    }
    if (error instanceof HttpErrorResponse && error.status === 400) {
      const details: unknown = error.error;
      if (details !== null && typeof details === 'object' &&
          'code' in details && details.code === 'INVALID_DASHBOARD_RANGE') {
        return 'Escolhe um intervalo de datas válido até cinco anos.';
      }
    }
    return 'Não foi possível carregar os dados do dashboard. Tenta novamente.';
  }

  private periodBounds(period: Period): DateRange | null {
    if (period === 'custom') {
      const range = this.customRange();
      return range ? { start: this.startOfDay(range[0]), end: this.endOfDay(range[1]) } : null;
    }

    const end = portugalToday();
    end.setHours(23, 59, 59, 999);
    const start = new Date(end);
    if (period === '30d') start.setDate(start.getDate() - 29);
    if (period === '90d') start.setDate(start.getDate() - 89);
    if (period === '12m') {
      start.setDate(1);
      start.setMonth(start.getMonth() - 11);
      start.setHours(0, 0, 0, 0);
    } else {
      start.setHours(0, 0, 0, 0);
    }
    return { start, end };
  }

  private startOfDay(date: Date): Date {
    const start = new Date(date);
    start.setHours(0, 0, 0, 0);
    return start;
  }

  private endOfDay(date: Date): Date {
    const end = new Date(date);
    end.setHours(23, 59, 59, 999);
    return end;
  }

  private formatDate(date: Date): string {
    return new Intl.DateTimeFormat(this.language.locale(), { day: '2-digit', month: '2-digit', year: 'numeric' }).format(date);
  }

  private buildChartSeries(daily: readonly DashboardDaily[], period: Period, bounds: DateRange | null): ChartSeries {
    const buckets = new Map<string, { label: string; revenue: number; profit: number }>();
    if (!bounds) return { labels: [], revenue: [], profit: [] };

    const spanDays = Math.round((
      Date.UTC(bounds.end.getFullYear(), bounds.end.getMonth(), bounds.end.getDate()) -
      Date.UTC(bounds.start.getFullYear(), bounds.start.getMonth(), bounds.start.getDate())
    ) / 86_400_000) + 1;
    const granularity = period === '30d' || (period === 'custom' && spanDays <= 31)
      ? 'day'
      : period === '90d' || (period === 'custom' && spanDays <= 120)
        ? 'week'
        : 'month';
    const cursor = new Date(bounds.start);
    const dayFormatter = new Intl.DateTimeFormat(this.language.locale(), { day: '2-digit', month: 'short' });
    const monthFormatter = new Intl.DateTimeFormat(this.language.locale(), {
      month: 'short',
      ...(period === 'custom' && spanDays > 365 ? { year: '2-digit' as const } : {}),
    });

    if (granularity === 'day') {
      for (let index = 0; index < spanDays; index++) {
        const date = new Date(cursor);
        date.setDate(cursor.getDate() + index);
        buckets.set(this.dateKey(date), { label: dayFormatter.format(date), revenue: 0, profit: 0 });
      }
    } else if (granularity === 'week') {
      cursor.setDate(cursor.getDate() - ((cursor.getDay() + 6) % 7));
      while (cursor <= bounds.end) {
        buckets.set(this.dateKey(cursor), { label: dayFormatter.format(cursor), revenue: 0, profit: 0 });
        cursor.setDate(cursor.getDate() + 7);
      }
    } else {
      cursor.setDate(1);
      while (cursor <= bounds.end) {
        const key = `${cursor.getFullYear()}-${String(cursor.getMonth() + 1).padStart(2, '0')}`;
        buckets.set(key, { label: monthFormatter.format(cursor), revenue: 0, profit: 0 });
        cursor.setMonth(cursor.getMonth() + 1);
      }
    }

    daily.forEach((point) => {
      const date = new Date(`${point.date}T12:00:00`);
      const key = granularity === 'month'
        ? `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`
        : this.dateKey(granularity === 'week' ? this.mondayOf(date) : date);
      const bucket = buckets.get(key);
      if (!bucket) return;
      bucket.revenue += point.revenue;
      bucket.profit += point.profit;
    });

    return {
      labels: [...buckets.values()].map((bucket) => bucket.label),
      revenue: [...buckets.values()].map((bucket) => bucket.revenue),
      profit: [...buckets.values()].map((bucket) => bucket.profit),
    };
  }

  private dateKey(date: Date): string {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }

  private mondayOf(date: Date): Date {
    const monday = new Date(date);
    monday.setDate(monday.getDate() - ((monday.getDay() + 6) % 7));
    return monday;
  }

  private updateCharts(
    profitCanvas: HTMLCanvasElement,
    sourceCanvas: HTMLCanvasElement,
    series: ChartSeries,
    sourceProfits: readonly SourceProfit[],
    theme: string,
    locale: string,
  ): void {
    if ((this.chartTheme !== null && this.chartTheme !== theme) || (this.chartLocale !== null && this.chartLocale !== locale)) {
      this.profitChart?.destroy();
      this.sourceChart?.destroy();
      this.profitChart = null;
      this.sourceChart = null;
    }
    this.chartTheme = theme;
    this.chartLocale = locale;

    const styles = getComputedStyle(document.documentElement);
    const textColor = styles.getPropertyValue('--app-text-muted').trim() || '#667085';
    const borderColor = styles.getPropertyValue('--app-border').trim() || '#e4e7ec';
    const currency = (value: number): string => new Intl.NumberFormat(locale, { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 }).format(value);

    if (!this.profitChart) {
      this.profitChart = new Chart(profitCanvas, {
        type: 'line',
        data: { labels: [...series.labels], datasets: [
          { label: this.language.translate('Receita'), data: [...series.revenue], borderColor: '#465fff', backgroundColor: 'rgba(70, 95, 255, 0.10)', fill: true, tension: 0.35, pointRadius: 2, pointHoverRadius: 5 },
          { label: this.language.translate('Lucro'), data: [...series.profit], borderColor: '#12b76a', backgroundColor: 'rgba(18, 183, 106, 0.08)', fill: false, tension: 0.35, pointRadius: 2, pointHoverRadius: 5 },
        ] },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          interaction: { mode: 'index', intersect: false },
          plugins: { legend: { position: 'top', align: 'end', labels: { usePointStyle: true, boxWidth: 7, color: textColor, padding: 18 } }, tooltip: { callbacks: { label: (item) => `${item.dataset.label}: ${new Intl.NumberFormat(locale, { style: 'currency', currency: 'EUR' }).format(item.parsed.y ?? 0)}` } } },
          scales: {
            x: { grid: { display: false }, ticks: { color: textColor, maxTicksLimit: 8, maxRotation: 0 } },
            y: { beginAtZero: true, grid: { color: borderColor }, ticks: { color: textColor, callback: (value) => currency(Number(value)) } },
          },
        },
      });
    } else {
      this.profitChart.data.labels = [...series.labels];
      this.profitChart.data.datasets[0].data = [...series.revenue];
      this.profitChart.data.datasets[1].data = [...series.profit];
      this.profitChart.update();
    }

    const sourceLabels = sourceProfits.map((source) => source.name);
    const sourceValues = sourceProfits.map((source) => source.profit);
    if (!this.sourceChart) {
      this.sourceChart = new Chart(sourceCanvas, {
        type: 'bar',
        data: { labels: sourceLabels, datasets: [{ label: this.language.translate('Lucro'), data: sourceValues, backgroundColor: '#465fff', borderRadius: 6, maxBarThickness: 24 }] },
        options: {
          indexAxis: 'y',
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { display: false }, tooltip: { callbacks: { label: (item) => new Intl.NumberFormat(locale, { style: 'currency', currency: 'EUR' }).format(item.parsed.x ?? 0) } } },
          scales: {
            x: { beginAtZero: true, grid: { color: borderColor }, ticks: { color: textColor, callback: (value) => currency(Number(value)) } },
            y: { grid: { display: false }, ticks: { color: textColor } },
          },
        },
      });
    } else {
      this.sourceChart.data.labels = sourceLabels;
      this.sourceChart.data.datasets[0].data = sourceValues;
      this.sourceChart.update();
    }
  }
}
