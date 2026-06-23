import { computed, onMounted, onUnmounted, ref } from 'vue';
import { adminNavigationItems, resolveAdminRouteKey, type AdminRouteKey } from '../navigation/adminNavigation';

function readHashPath(): string {
  const raw = window.location.hash.replace(/^#/, '');
  return raw.startsWith('/') ? raw : '/overview';
}

export function useAdminRouter() {
  const path = ref(readHashPath());

  function syncFromLocation(): void {
    path.value = readHashPath();
  }

  function navigate(nextPath: string): void {
    const normalized = nextPath.startsWith('/') ? nextPath : `/${nextPath}`;
    window.location.hash = normalized;
    path.value = normalized;
  }

  onMounted(() => {
    if (!window.location.hash) {
      window.location.hash = '/overview';
    }
    syncFromLocation();
    window.addEventListener('hashchange', syncFromLocation);
  });

  onUnmounted(() => window.removeEventListener('hashchange', syncFromLocation));

  const routeKey = computed<AdminRouteKey>(() => resolveAdminRouteKey(path.value));
  const currentRoute = computed(() => adminNavigationItems.find(item => item.key === routeKey.value) || adminNavigationItems[0]);

  return {
    path,
    routeKey,
    currentRoute,
    navigate
  };
}
