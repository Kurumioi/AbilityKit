import type { ApiResult } from '../types';

const defaultApiBaseUrl = import.meta.env.VITE_ABILITYKIT_GATEWAY_URL || '';

export class AdminApiClient {
  public constructor(private readonly baseUrl: string = defaultApiBaseUrl) {
  }

  public async request<T>(url: string, body?: unknown, method?: string): Promise<ApiResult<T>> {
    const requestMethod = method || (body === undefined ? 'GET' : 'POST');
    const startedAt = performance.now();
    const options: RequestInit = {
      method: requestMethod,
      headers: {}
    };

    if (body !== undefined) {
      (options.headers as Record<string, string>)['Content-Type'] = 'application/json';
      options.body = JSON.stringify(body);
    }

    const response = await fetch(`${this.baseUrl}${url}`, options);
    const text = await response.text();
    let data: T | string | null = null;
    try {
      data = text ? JSON.parse(text) as T : null;
    } catch {
      data = text;
    }

    return {
      ok: response.ok,
      status: response.status,
      statusText: response.statusText,
      method: requestMethod,
      url,
      durationMs: Math.round(performance.now() - startedAt),
      body: data
    };
  }
}
