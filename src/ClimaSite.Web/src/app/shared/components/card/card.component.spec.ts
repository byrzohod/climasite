import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CardComponent } from './card.component';

describe('CardComponent', () => {
  let component: CardComponent;
  let fixture: ComponentFixture<CardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CardComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(CardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have base card class', () => {
    const card = fixture.nativeElement.querySelector('.card');
    expect(card).toBeTruthy();
  });

  it('should apply hoverable class when hoverable is true', () => {
    fixture.componentRef.setInput('hoverable', true);
    fixture.detectChanges();

    const card = fixture.nativeElement.querySelector('.card');
    expect(card.classList.contains('card-hoverable')).toBeTrue();
  });

  it('should apply clickable class when clickable is true', () => {
    fixture.componentRef.setInput('clickable', true);
    fixture.detectChanges();

    const card = fixture.nativeElement.querySelector('.card');
    expect(card.classList.contains('card-clickable')).toBeTrue();
  });

  it('should apply bordered class when bordered is true', () => {
    fixture.componentRef.setInput('bordered', true);
    fixture.detectChanges();

    const card = fixture.nativeElement.querySelector('.card');
    expect(card.classList.contains('card-bordered')).toBeTrue();
  });

  it('should have correct data-testid', () => {
    fixture.componentRef.setInput('testId', 'product-card');
    fixture.detectChanges();

    const card = fixture.nativeElement.querySelector('.card');
    expect(card.getAttribute('data-testid')).toBe('product-card');
  });
});
