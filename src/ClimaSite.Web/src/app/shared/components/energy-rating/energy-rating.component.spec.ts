import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EnergyRatingComponent, EnergyRatingLevel } from './energy-rating.component';
import { TranslateModule } from '@ngx-translate/core';

describe('EnergyRatingComponent', () => {
  let component: EnergyRatingComponent;
  let fixture: ComponentFixture<EnergyRatingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EnergyRatingComponent, TranslateModule.forRoot()]
    }).compileComponents();

    fixture = TestBed.createComponent(EnergyRatingComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.componentRef.setInput('rating', 'A++');
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should display the correct rating', () => {
    fixture.componentRef.setInput('rating', 'A+');
    fixture.detectChanges();
    expect(component.rating()).toBe('A+');
  });

  it('should return correct color for each rating level', () => {
    const testCases: { rating: EnergyRatingLevel; expectedColor: string }[] = [
      { rating: 'A+++', expectedColor: '#00A651' },
      { rating: 'A++', expectedColor: '#50B848' },
      { rating: 'A+', expectedColor: '#B6D433' },
      { rating: 'A', expectedColor: '#FEF200' },
      { rating: 'B', expectedColor: '#FBBA00' },
      { rating: 'C', expectedColor: '#F37021' },
      { rating: 'D', expectedColor: '#ED1C24' },
      { rating: 'E', expectedColor: '#E30613' },
      { rating: 'F', expectedColor: '#C7132A' },
      { rating: 'G', expectedColor: '#A11131' }
    ];

    testCases.forEach(({ rating, expectedColor }) => {
      fixture.componentRef.setInput('rating', rating);
      fixture.detectChanges();
      expect(component.getColor(rating)).toBe(expectedColor);
    });
  });

  it('should have all energy rating levels', () => {
    fixture.componentRef.setInput('rating', 'A');
    fixture.detectChanges();
    expect(component.levels).toEqual(['A+++', 'A++', 'A+', 'A', 'B', 'C', 'D', 'E', 'F', 'G']);
  });

  it('should return correct width for each rating level', () => {
    fixture.componentRef.setInput('rating', 'A+++');
    fixture.detectChanges();
    expect(component.getWidth('A+++')).toBe('40%');
    expect(component.getWidth('G')).toBe('100%');
  });

  it('should calculate rating index correctly', () => {
    fixture.componentRef.setInput('rating', 'A');
    fixture.detectChanges();
    expect(component.ratingIndex()).toBe(3);

    fixture.componentRef.setInput('rating', 'A+++');
    fixture.detectChanges();
    expect(component.ratingIndex()).toBe(0);
  });

  it('should use custom label when provided', () => {
    fixture.componentRef.setInput('rating', 'A++');
    fixture.componentRef.setInput('label', 'products.coolingEfficiency');
    fixture.detectChanges();
    expect(component.label()).toBe('products.coolingEfficiency');
  });

  it('should render the rating scale with all levels', () => {
    fixture.componentRef.setInput('rating', 'B');
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const levels = compiled.querySelectorAll('.level');
    expect(levels.length).toBe(10);
  });

  it('should highlight the active rating level', () => {
    fixture.componentRef.setInput('rating', 'A+');
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const activeLevel = compiled.querySelector('.level.active');
    expect(activeLevel).toBeTruthy();
    expect(activeLevel.textContent).toContain('A+');
  });
});
