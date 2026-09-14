import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SourceForm } from './source-form.component';

describe('SourceForm', () => {
  let component: SourceForm;
  let fixture: ComponentFixture<SourceForm>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SourceForm],
    }).compileComponents();

    fixture = TestBed.createComponent(SourceForm);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
