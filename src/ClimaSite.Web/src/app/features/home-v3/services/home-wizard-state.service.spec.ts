import { TestBed } from '@angular/core/testing';
import { HomeWizardStateService } from './home-wizard-state.service';

describe('HomeWizardStateService', () => {
  let service: HomeWizardStateService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(HomeWizardStateService);
  });

  it('starts with the default living-room / 24 m² / zone B configuration', () => {
    expect(service.area()).toBe(24);
    expect(service.roomType()).toBe('living');
    expect(service.zone()).toBe('B');
  });

  it('clamps area to the [10, 120] range', () => {
    service.setArea(5);
    expect(service.area()).toBe(10);
    service.setArea(500);
    expect(service.area()).toBe(120);
    service.setArea(45);
    expect(service.area()).toBe(45);
  });

  it('rounds non-integer area inputs', () => {
    service.setArea(33.7);
    expect(service.area()).toBe(34);
  });

  it('updates room type independently of area', () => {
    service.setArea(60);
    service.setRoomType('bedroom');
    expect(service.roomType()).toBe('bedroom');
    expect(service.area()).toBe(60);
  });

  it('updates zone independently of other state', () => {
    service.setZone('C');
    expect(service.zone()).toBe('C');
    expect(service.area()).toBe(24);
    expect(service.roomType()).toBe('living');
  });

  it('estimates BTU using zone multipliers (A=90, B=110, C=140)', () => {
    service.setArea(20);
    service.setZone('A');
    expect(service.estimatedBtu()).toBe(1800);

    service.setZone('B');
    expect(service.estimatedBtu()).toBe(2200);

    service.setZone('C');
    expect(service.estimatedBtu()).toBe(2800);
  });

  it('returns a lower target inside temperature for bedrooms', () => {
    service.setRoomType('living');
    expect(service.insideTargetC()).toBe(23);
    service.setRoomType('bedroom');
    expect(service.insideTargetC()).toBe(21);
  });

  it('reflects zone in the outside-sample temperature', () => {
    service.setZone('A');
    expect(service.outsideSampleC()).toBe(32);
    service.setZone('B');
    expect(service.outsideSampleC()).toBe(28);
    service.setZone('C');
    expect(service.outsideSampleC()).toBe(-12);
  });

  it('estimates wattage as ~9% of BTU', () => {
    service.setArea(20);
    service.setZone('B'); // 2200 BTU
    expect(service.estimatedWatts()).toBe(Math.round(2200 * 0.09));
  });
});
