// One confirmation of "start sending" is one attempt. Every try of that
// attempt (for example a retry after a network error) sends the same
// Idempotency-Key, so the server can replay the stored answer instead of
// starting twice; a new confirmation gets a fresh key.
export class AttemptKey {
  private current: string | null = null;

  constructor(
    private readonly create: () => string = () => crypto.randomUUID(),
  ) {}

  // Key to send now: the one of the running attempt, or a new one.
  next(): string {
    this.current ??= this.create();
    return this.current;
  }

  // The attempt ended for good (it succeeded, or the dialog was closed).
  finish(): void {
    this.current = null;
  }
}
