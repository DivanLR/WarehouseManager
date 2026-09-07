import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthResponse } from './auth-response';
import { CreateProductRequest } from './create-product-request';
import { LoginRequest } from './login-request';
import { Product } from './product';
import { SuccessResponse } from './success-response';

@Injectable({ providedIn: 'root' })
export class WarehouseApi {
  private readonly http = inject(HttpClient);

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/login', request);
  }

  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>('/api/products');
  }

  createProduct(request: CreateProductRequest): Observable<SuccessResponse> {
    return this.http.post<SuccessResponse>('/api/products', request);
  }
}
