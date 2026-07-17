import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { authGuard } from './auth.guard';
import { Login } from './login/login';
import { Tasks } from './tasks/tasks';

const routes: Routes = [
  { path: 'login', component: Login },
  { path: '', component: Tasks, canActivate: [authGuard] },
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
