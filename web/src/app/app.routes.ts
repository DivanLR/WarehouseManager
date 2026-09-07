import { Routes } from '@angular/router';
import { authGuard } from './auth/auth-guard';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Sign in',
    loadComponent: () => import('./auth/login/login').then(m => m.Login)
  },
  {
    path: 'products',
    title: 'Products',
    canActivate: [authGuard],
    loadComponent: () => import('./products/product-list/product-list').then(m => m.ProductList)
  },
  {
    path: 'products/new',
    title: 'New product',
    canActivate: [authGuard],
    loadComponent: () => import('./products/product-create/product-create').then(m => m.ProductCreate)
  },
  { path: '', pathMatch: 'full', redirectTo: 'products' },
  { path: '**', redirectTo: 'products' }
];
