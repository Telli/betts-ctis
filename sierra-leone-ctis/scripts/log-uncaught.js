// Simple helper to capture uncaught exception stacks during Next.js build
process.on('uncaughtException', (err) => {
  console.error('\n[log-uncaught] Uncaught exception:', err);
  if (err && err.stack) {
    console.error('\n[log-uncaught] Stack trace:\n', err.stack);
  }
  // Do not exit here; allow Next.js' own handlers / default behavior to run as well.
});

