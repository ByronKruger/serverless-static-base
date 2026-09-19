import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzResultModule } from 'ng-zorro-antd/result';

/** Rendered for any route the application does not know. */
@Component({
  selector: 'app-not-found',
  imports: [RouterLink, NzResultModule, NzButtonModule],
  templateUrl: './not-found.html',
})
export class NotFound {}
