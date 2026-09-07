import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { Product } from '../../api/product';
import { problemMessage } from '../../api/problem-details-extensions';
import { WarehouseApi } from '../../api/warehouse-api';

@Component({
  selector: 'app-product-list',
  imports: [RouterLink, MatButtonModule, MatCardModule, MatProgressSpinnerModule, MatTableModule],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss'
})
export class ProductList {
  private readonly api = inject(WarehouseApi);

  protected readonly products = signal<Product[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly columns = ['code', 'description'];

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.getProducts().subscribe({
      next: products => {
        this.products.set(products);
        this.loading.set(false);
      },
      error: (failure: unknown) => {
        this.error.set(problemMessage(failure, 'Could not load products.'));
        this.loading.set(false);
      }
    });
  }
}
