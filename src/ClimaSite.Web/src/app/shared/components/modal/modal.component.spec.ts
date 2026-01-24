import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ModalComponent } from './modal.component';
import { TranslateModule } from '@ngx-translate/core';
import { By } from '@angular/platform-browser';
import { Component } from '@angular/core';

@Component({
  standalone: true,
  imports: [ModalComponent],
  template: `
    <app-modal [isOpen]="isOpen" (closed)="onClosed()">
      <ng-container modal-header>Test Title</ng-container>
      <ng-container modal-body>Test Body Content</ng-container>
      <ng-container modal-footer>
        <button id="footer-btn">Footer Button</button>
      </ng-container>
    </app-modal>
  `
})
class TestHostComponent {
  isOpen = false;
  closedCount = 0;
  onClosed() {
    this.closedCount++;
    this.isOpen = false;
  }
}

describe('ModalComponent', () => {
  let component: ModalComponent;
  let fixture: ComponentFixture<ModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ModalComponent, TranslateModule.forRoot()]
    }).compileComponents();

    fixture = TestBed.createComponent(ModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    // Clean up body overflow style
    document.body.style.overflow = '';
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should not render modal when isOpen is false', () => {
    fixture.componentRef.setInput('isOpen', false);
    fixture.detectChanges();
    
    const backdrop = fixture.debugElement.query(By.css('.modal-backdrop'));
    expect(backdrop).toBeFalsy();
  });

  it('should render modal when isOpen is true', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();
    
    const backdrop = fixture.debugElement.query(By.css('.modal-backdrop'));
    expect(backdrop).toBeTruthy();
  });

  it('should have role="dialog" and aria-modal="true"', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();
    
    const dialog = fixture.debugElement.query(By.css('[role="dialog"]'));
    expect(dialog).toBeTruthy();
    expect(dialog.nativeElement.getAttribute('aria-modal')).toBe('true');
  });

  it('should emit closed event when close button is clicked', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();
    
    const closedSpy = spyOn(component.closed, 'emit');
    const closeButton = fixture.debugElement.query(By.css('.modal-close'));
    
    closeButton.nativeElement.click();
    
    expect(closedSpy).toHaveBeenCalled();
  });

  it('should emit closed event when backdrop is clicked', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.componentRef.setInput('closeOnBackdropClick', true);
    fixture.detectChanges();
    
    const closedSpy = spyOn(component.closed, 'emit');
    const backdrop = fixture.debugElement.query(By.css('.modal-backdrop'));
    
    backdrop.nativeElement.click();
    
    expect(closedSpy).toHaveBeenCalled();
  });

  it('should not emit closed event when backdrop click is disabled', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.componentRef.setInput('closeOnBackdropClick', false);
    fixture.detectChanges();
    
    const closedSpy = spyOn(component.closed, 'emit');
    const backdrop = fixture.debugElement.query(By.css('.modal-backdrop'));
    
    backdrop.nativeElement.click();
    
    expect(closedSpy).not.toHaveBeenCalled();
  });

  it('should not close when clicking inside the modal container', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();
    
    const closedSpy = spyOn(component.closed, 'emit');
    const container = fixture.debugElement.query(By.css('.modal-container'));
    
    container.nativeElement.click();
    
    expect(closedSpy).not.toHaveBeenCalled();
  });

  it('should hide close button when showCloseButton is false', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.componentRef.setInput('showCloseButton', false);
    fixture.detectChanges();
    
    const closeButton = fixture.debugElement.query(By.css('.modal-close'));
    expect(closeButton).toBeFalsy();
  });

  it('should apply custom testId', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.componentRef.setInput('testId', 'custom-modal');
    fixture.detectChanges();
    
    const modal = fixture.debugElement.query(By.css('[data-testid="custom-modal"]'));
    expect(modal).toBeTruthy();
  });

  it('should close on Escape key press', fakeAsync(() => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.componentRef.setInput('closeOnEscape', true);
    fixture.detectChanges();
    tick();
    
    const closedSpy = spyOn(component.closed, 'emit');
    
    const event = new KeyboardEvent('keydown', { key: 'Escape' });
    document.dispatchEvent(event);
    
    expect(closedSpy).toHaveBeenCalled();
  }));

  it('should not close on Escape when closeOnEscape is false', fakeAsync(() => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.componentRef.setInput('closeOnEscape', false);
    fixture.detectChanges();
    tick();
    
    const closedSpy = spyOn(component.closed, 'emit');
    
    const event = new KeyboardEvent('keydown', { key: 'Escape' });
    document.dispatchEvent(event);
    
    expect(closedSpy).not.toHaveBeenCalled();
  }));

  it('should prevent body scroll when modal is open', fakeAsync(() => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();
    tick();
    
    expect(document.body.style.overflow).toBe('hidden');
  }));

  it('should restore body scroll when modal is closed', fakeAsync(() => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();
    tick();
    
    fixture.componentRef.setInput('isOpen', false);
    fixture.detectChanges();
    tick();
    
    expect(document.body.style.overflow).toBe('');
  }));
});

describe('ModalComponent with TestHost', () => {
  let hostFixture: ComponentFixture<TestHostComponent>;
  let hostComponent: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent, TranslateModule.forRoot()]
    }).compileComponents();

    hostFixture = TestBed.createComponent(TestHostComponent);
    hostComponent = hostFixture.componentInstance;
    hostFixture.detectChanges();
  });

  afterEach(() => {
    document.body.style.overflow = '';
  });

  it('should render projected content', fakeAsync(() => {
    hostComponent.isOpen = true;
    hostFixture.detectChanges();
    tick();
    
    const title = hostFixture.debugElement.query(By.css('.modal-title'));
    const body = hostFixture.debugElement.query(By.css('.modal-body'));
    const footerBtn = hostFixture.debugElement.query(By.css('#footer-btn'));
    
    expect(title.nativeElement.textContent).toContain('Test Title');
    expect(body.nativeElement.textContent).toContain('Test Body Content');
    expect(footerBtn).toBeTruthy();
  }));

  it('should handle closed event in parent', fakeAsync(() => {
    hostComponent.isOpen = true;
    hostFixture.detectChanges();
    tick();
    
    const closeButton = hostFixture.debugElement.query(By.css('.modal-close'));
    closeButton.nativeElement.click();
    hostFixture.detectChanges();
    tick();
    
    expect(hostComponent.closedCount).toBe(1);
    expect(hostComponent.isOpen).toBe(false);
  }));
});
