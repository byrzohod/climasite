import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SpecsTableComponent, SpecificationGroup } from './specs-table.component';
import { TranslateModule } from '@ngx-translate/core';

describe('SpecsTableComponent', () => {
  let component: SpecsTableComponent;
  let fixture: ComponentFixture<SpecsTableComponent>;

  const mockSpecs: Record<string, unknown> = {
    cooling_capacity: '9000 BTU',
    heating_capacity: '10000 BTU',
    power: '2500W',
    noise_level: '45dB',
    energy_class: 'A++',
    refrigerant: 'R32',
    dimensions: '800x300x200mm',
    weight: '35kg',
    color: 'White',
    warranty: '24 months'
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        SpecsTableComponent,
        TranslateModule.forRoot()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SpecsTableComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('specifications', mockSpecs);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display specs table', () => {
    const compiled = fixture.nativeElement;
    const table = compiled.querySelector('[data-testid="specs-table"]');
    expect(table).toBeTruthy();
  });

  it('should display specification rows', () => {
    const compiled = fixture.nativeElement;
    const rows = compiled.querySelectorAll('tr');
    expect(rows.length).toBeGreaterThan(0);
  });

  it('should limit initial display to 8 items by default', () => {
    const compiled = fixture.nativeElement;
    const rows = compiled.querySelectorAll('tr');
    expect(rows.length).toBe(8);
  });

  it('should show "Show More" button when more specs available', () => {
    const compiled = fixture.nativeElement;
    const showMore = compiled.querySelector('[data-testid="show-more-specs"]');
    expect(showMore).toBeTruthy();
  });

  it('should show all specs when Show More is clicked', () => {
    component.toggleShowAll();
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const rows = compiled.querySelectorAll('tr');
    expect(rows.length).toBe(10);
  });

  it('should format boolean values', () => {
    expect(component.formatValue(true)).toBe('Yes');
    expect(component.formatValue(false)).toBe('No');
  });

  it('should format array values', () => {
    expect(component.formatValue(['a', 'b', 'c'])).toBe('a, b, c');
  });

  it('should format null/undefined as dash', () => {
    expect(component.formatValue(null)).toBe('-');
    expect(component.formatValue(undefined)).toBe('-');
  });

  it('should highlight specified keys', () => {
    fixture.componentRef.setInput('highlightKeys', ['energy_class']);
    fixture.detectChanges();

    expect(component.isHighlighted('energy_class')).toBeTrue();
    expect(component.isHighlighted('power')).toBeFalse();
  });

  it('should display search input when searchable is true', () => {
    fixture.componentRef.setInput('searchable', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const searchInput = compiled.querySelector('[data-testid="specs-search"]');
    expect(searchInput).toBeTruthy();
  });

  it('should filter specs by search term', () => {
    fixture.componentRef.setInput('searchable', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const searchInput = compiled.querySelector('[data-testid="specs-search"]');

    searchInput.value = 'energy';
    searchInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const filteredSpecs = component.filteredSpecs();
    expect(filteredSpecs.length).toBe(1);
    expect(filteredSpecs[0].key).toBe('energy_class');
  });

  it('should display grouped specs when groups are provided', () => {
    const groups: SpecificationGroup[] = [
      {
        name: 'Performance',
        specs: { cooling_capacity: '9000 BTU', heating_capacity: '10000 BTU' }
      },
      {
        name: 'Physical',
        specs: { dimensions: '800x300x200mm', weight: '35kg' }
      }
    ];

    fixture.componentRef.setInput('groups', groups);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const groupTitles = compiled.querySelectorAll('.group-title');
    expect(groupTitles.length).toBe(2);
  });

  it('should get unit for specification key', () => {
    fixture.componentRef.setInput('units', {
      weight: 'kg',
      power: 'W'
    });
    fixture.detectChanges();

    expect(component.getUnit('weight')).toBe('kg');
    expect(component.getUnit('power')).toBe('W');
    expect(component.getUnit('color')).toBeUndefined();
  });

  it('should detect long values', () => {
    expect(component.hasLongValue('Short')).toBeFalse();
    expect(component.hasLongValue('A'.repeat(100))).toBeTrue();
  });

  it('should truncate long values', () => {
    const longValue = 'A'.repeat(100);
    const truncated = component.truncateValue(longValue);
    expect(truncated.length).toBe(50);
  });

  it('should toggle expanded state for long values', () => {
    expect(component.expandedKeys().has('test')).toBeFalse();

    component.toggleExpand('test');
    expect(component.expandedKeys().has('test')).toBeTrue();

    component.toggleExpand('test');
    expect(component.expandedKeys().has('test')).toBeFalse();
  });

  it('should respect custom initial count', () => {
    fixture.componentRef.setInput('initialCount', 5);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const rows = compiled.querySelectorAll('tr');
    expect(rows.length).toBe(5);
  });

  it('should hide show more when showMore is false', () => {
    fixture.componentRef.setInput('showMore', false);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const showMore = compiled.querySelector('[data-testid="show-more-specs"]');
    expect(showMore).toBeFalsy();
  });

  it('should calculate remaining count', () => {
    expect(component.remainingCount()).toBe(2); // 10 total - 8 initial
  });
});
