// Serialize all local file/key operations; queued writes from a cleared session are discarded.
export class CacheQueue {
  private tail: Promise<unknown> = Promise.resolve();
  private epoch = 0;
  run<T>(task: () => Promise<T>): Promise<T> { const result = this.tail.then(task); this.tail = result.catch(() => undefined); return result; }
  write<T>(task: () => Promise<T>, stale: T): Promise<T> { const epoch = this.epoch; return this.run(() => epoch === this.epoch ? task() : Promise.resolve(stale)); }
  clear(task: () => Promise<void>): Promise<void> { this.epoch++; return this.run(task); }
}
