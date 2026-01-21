var items = $props.items.filter((it) => {return it.status !== 'error'});
var sum = items.reduce((acc, item) => acc + item.item.file.size, 0);
var loaded = items.reduce((acc, item) => acc + (item.item.file.size * item.progress.progress || 0), 0);
var progress = loaded / sum;