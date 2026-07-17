import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Tasks } from './tasks';

describe('Tasks form validation', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormsModule, ReactiveFormsModule],
      declarations: [Tasks],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('rejects an empty or whitespace title on submit without calling the API', () => {
    const cmp = TestBed.createComponent(Tasks).componentInstance;

    cmp.save();
    expect(cmp.titleInvalid()).toBe(true);

    cmp.form.controls.title.setValue('   ');
    cmp.save();
    expect(cmp.titleInvalid()).toBe(true);

    TestBed.inject(HttpTestingController).verify();
  });

  it('editing a task leaves the add form untouched', () => {
    const cmp = TestBed.createComponent(Tasks).componentInstance;

    cmp.form.controls.title.setValue('draft in progress');
    cmp.edit({ id: 5, title: 'Existing', description: '', status: 'Done', dueDate: null, userId: 1 });

    expect(cmp.form.controls.title.value).toBe('draft in progress');
    expect(cmp.editForm.controls.title.value).toBe('Existing');
    expect(cmp.editingId()).toBe(5);
    TestBed.inject(HttpTestingController).verify();
  });
});
