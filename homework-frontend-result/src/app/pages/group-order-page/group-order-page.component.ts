import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, map, switchMap } from 'rxjs';
import { ParticipantCardComponent } from '@app/components/group-orders/participant-card/participant-card.component';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { ToastService } from '@app/services/toast/toast.service';
import { consolidatePreOrder } from '@app/shared/helpers/consolidate-pre-order';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { Participant } from '@app/models/group-orders/participant.model';
import { GroupReservationStatus } from '@app/models/group-orders/group-status.model';

type LoadState = 'loading' | 'ready' | 'not-found' | 'error';

const STATUS_LABELS: Record<GroupReservationStatus, string> = {
  Open: 'Collecting pre-orders',
  Submitted: 'Sent to Restaurant',
  Closed: 'Closed',
};

@Component({
  selector: 'app-group-order-page',
  imports: [ParticipantCardComponent, ModalComponent, DatePipe, CurrencyPipe],
  templateUrl: './group-order-page.component.html',
  styleUrl: './group-order-page.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class GroupOrderPageComponent {
  private readonly groupOrders = inject(GroupOrdersService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reservationId = inject(ActivatedRoute).snapshot.paramMap.get('reservationId') ?? '';

  // стан екрана
  readonly state = signal<LoadState>('loading');
  readonly invite = signal<GuestInvite | null>(null);
  readonly participants = signal<Participant[]>([]);
  readonly ownerSessionId = signal('');
  readonly status = signal<GroupReservationStatus>('Open');
  readonly isConfirmOpen = signal(false);
  readonly isSubmitting = signal(false);

  // похідні значення — перераховуються самі, коли змінюються participants / status
  readonly consolidated = computed(() => consolidatePreOrder(this.participants()));
  /** Тільки підтверджені кошики — саме це піде в ресторан, а не `total` з API. */
  readonly grandTotal = computed(() => this.consolidated().total);
  readonly pendingCount = computed(() => this.participants().filter((person) => !person.isSubmitted).length);
  readonly pendingMessage = computed(() => {
    const count = this.pendingCount();
    const who = count === 1 ? '1 participant hasn’t' : `${count} participants haven’t`;
    return `${who} submitted their cart yet — their dishes won’t be included in the order`;
  });
  readonly hasAnyDishes = computed(() => this.participants().some((person) => person.items.length > 0));
  readonly isSubmitted = computed(() => this.status() !== 'Open');
  readonly statusLabel = computed(() => STATUS_LABELS[this.status()]);
  readonly canSubmit = computed(() => this.consolidated().items.length > 0 && !this.isSubmitting());

  constructor() {
    this.load();
  }

  load(): void {
    this.state.set('loading');
    this.groupOrders
      .getGroupInvite$(this.reservationId)
      .pipe(
        switchMap((invite) =>
          forkJoin({
            sessions: this.groupOrders.getSessions$(invite.GroupReservationId),
            ownerSessionId: this.groupOrders.getOwnerSessionId$(this.reservationId),
            status: this.groupOrders.getStatus$(invite.GroupReservationId),
          }).pipe(map((data) => ({ invite, ...data }))),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ invite, sessions, ownerSessionId, status }) => {
          this.invite.set(invite);
          this.participants.set(sessions.participants);
          this.ownerSessionId.set(ownerSessionId);
          this.status.set(status.status);
          this.state.set('ready');
        },
        error: (error: HttpErrorResponse) => this.state.set(error.status === 404 ? 'not-found' : 'error'),
      });
  }

  openConfirm(): void {
    this.isConfirmOpen.set(true);
  }

  closeConfirm(): void {
    if (!this.isSubmitting()) {
      this.isConfirmOpen.set(false);
    }
  }

  submit(): void {
    const groupId = this.invite()?.GroupReservationId;
    if (!groupId || this.isSubmitting()) {
      return; // захист від подвійного кліку
    }
    this.isSubmitting.set(true);
    this.groupOrders
      .submit$(groupId, this.reservationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.isConfirmOpen.set(false);
          this.status.set('Submitted');
          this.toast.success('Pre-order sent to the restaurant');
        },
        // toast з помилкою вже показує tokenInterceptor — тут лише повертаємо екран у робочий стан
        error: () => {
          this.isSubmitting.set(false);
          this.isConfirmOpen.set(false);
        },
      });
  }
}
