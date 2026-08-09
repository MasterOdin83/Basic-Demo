import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { authGuard } from './auth.guard';
import { Home } from './home/home';
import { Tasks } from './tasks/tasks';

// No standalone /login route — the Login component only ever renders inside the
// topbar's drawer dialog (see app.html); a bare page for it was unused surface.
const routes: Routes = [
  { path: '', component: Home },
  { path: 'tasks', component: Tasks, canActivate: [authGuard] },
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
