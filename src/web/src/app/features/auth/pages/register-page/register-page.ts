import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { AuthStore } from '../../../../core/auth/auth.store';
import { ApiError, fieldErrorsFor, isApiError } from '../../../../core/http/api-error';

/**
 * Create an account. Email is the unique identity for a person; the username is what
 * the rest of the app shows, and is unique too. The two names are what makes somebody
 * findable by a person who knows them rather than their handle (D-0018).
 */
@Component({
  selector: 'app-register-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    NzFormModule,
    NzInputModule,
    NzButtonModule,
    NzAlertModule,
    NzIconModule,
  ],
  templateUrl: './register-page.html',
})
export class RegisterPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    userName: ['', [Validators.required, Validators.minLength(3)]],
    // Required here and at the API, though the columns stay nullable so the accounts
    // that predate them survive (D-0016). No pattern: a name may carry apostrophes,
    // hyphens, spaces and accents, unlike a username.
    firstName: ['', [Validators.required, Validators.maxLength(64)]],
    lastName: ['', [Validators.required, Validators.maxLength(64)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  protected readonly submitting = signal(false);
  protected readonly error = signal<ApiError | null>(null);

  protected fieldErrors(field: string): readonly string[] {
    return fieldErrorsFor(this.error(), field);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => void this.router.navigate(['/']),
      error: (error: unknown) => {
        this.submitting.set(false);

        if (isApiError(error)) {
          this.error.set(error);
          return;
        }

        // Nothing this page can explain - let the global handler report it.
        throw error;
      },
    });
  }
}
