import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { Participant } from '@app/models/group-orders/participant.model';

@Component({
  selector: 'app-participant-card',
  imports: [CurrencyPipe],
  templateUrl: './participant-card.component.html',
  styleUrl: './participant-card.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class ParticipantCardComponent {
  readonly participant = input.required<Participant>();
  /** true — це картка самого Owner, що дивиться на екран. */
  readonly isMe = input(false);

  readonly title = computed(() => {
    const person = this.participant();
    if (this.isMe()) {
      return 'My Pre-Order';
    }
    return person.type === 'Owner' ? `Owner: ${person.name}` : `Guest: ${person.name}`;
  });
}
