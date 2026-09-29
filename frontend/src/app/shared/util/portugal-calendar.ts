const portugalDateFormatter = new Intl.DateTimeFormat('en-GB', {
  timeZone: 'Europe/Lisbon',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
});

export function portugalToday(now = new Date()): Date {
  const parts = portugalDateFormatter.formatToParts(now);
  const number = (type: 'year' | 'month' | 'day'): number =>
    Number(parts.find((part) => part.type === type)?.value);

  // A Date here represents a calendar day, not an instant. The UI's existing
  // date arithmetic and date picker use its local year/month/day fields.
  return new Date(number('year'), number('month') - 1, number('day'));
}

export function portugalSaleDateInstant(date: string): string {
  // Noon UTC is always within the same calendar day in Europe/Lisbon,
  // including both sides of the daylight-saving transitions.
  return `${date}T12:00:00.000Z`;
}
