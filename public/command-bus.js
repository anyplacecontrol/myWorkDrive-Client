// Command-bus helpers moved out of inline index.html
// UUID generator for command bus
window.newUuid = () => {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  // Fallback for environments without crypto.randomUUID
  return `${Date.now()}-${Math.random().toString().slice(2)}`;
};

// Global command batching for CommandBus
window.__commandBatch = [];

window.batchCommand = (cmd, updateFn) => {
  window.__commandBatch.push(cmd);

  setTimeout(() => {
    if (window.__commandBatch.length > 0) {
      const commandsToFlush = [...window.__commandBatch];
      window.__commandBatch = [];
      updateFn(commandsToFlush);
    }
  }, 0);
};
