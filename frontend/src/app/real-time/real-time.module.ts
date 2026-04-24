import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RealtimeStatusComponent } from './realtime-status.component';

@NgModule({
  declarations: [RealtimeStatusComponent],
  imports: [CommonModule],
  exports: [RealtimeStatusComponent]
})
export class RealTimeModule {}
