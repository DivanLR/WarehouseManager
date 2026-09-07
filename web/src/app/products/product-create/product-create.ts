import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router, RouterLink } from '@angular/router';
import { problemMessage, serverFieldErrors } from '../../api/problem-details-extensions';
import { WarehouseApi } from '../../api/warehouse-api';

@Component({
  selector: 'app-product-create',
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './product-create.html',
  styleUrl: './product-create.scss'
})
export class ProductCreate {
  private readonly api = inject(WarehouseApi);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(50)]],
    description: ['', [Validators.required, Validators.maxLength(255)]]
  });

  protected submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();

      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    const request = this.form.getRawValue();

    this.api.createProduct(request).subscribe({
      next: response => {
        this.snackBar.open(response.message, 'Dismiss', { duration: 4000 });
        this.router.navigate(['/products']);
      },
      error: (failure: unknown) => {
        this.submitting.set(false);

        const fieldErrors = serverFieldErrors(failure);

        for (const [field, message] of Object.entries(fieldErrors)) {
          this.form.get(field)?.setErrors({ server: message });
        }

        if (Object.keys(fieldErrors).length === 0) {
          this.error.set(problemMessage(failure, 'Could not create the product.'));
        }
      }
    });
  }
}
