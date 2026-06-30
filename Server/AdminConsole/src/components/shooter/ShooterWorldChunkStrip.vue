<template>
  <div class="chunk-strip">
    <article v-for="chunk in chunks" :key="`${chunk.entityKind}-${chunk.componentKind}`" class="chunk-card">
      <strong>{{ chunk.componentKind }}</strong>
      <small>{{ chunk.entityKind }} / {{ chunk.count }} rows</small>
    </article>
  </div>
</template>

<script setup lang="ts">
import type { ShooterWorldComponentChunkDiagnostics } from '../../types';

defineProps<{
  chunks: ShooterWorldComponentChunkDiagnostics[];
}>();
</script>
