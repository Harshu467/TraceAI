import type { FaqCategory, FaqItem, PublicPage } from '../types/public';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000/api';

export async function getPublicPage(slug: string): Promise<PublicPage> {
  const response = await fetch(`${API_BASE}/public/pages/${slug}`);
  if (!response.ok) {
    throw new Error('Failed to load page content');
  }

  return response.json();
}

export async function getFaqCategories(): Promise<FaqCategory[]> {
  const response = await fetch(`${API_BASE}/public/faq/categories`);
  if (!response.ok) {
    throw new Error('Failed to load FAQ categories');
  }

  return response.json();
}

export async function getFaqItems(categoryId?: string, search?: string): Promise<FaqItem[]> {
  const query = new URLSearchParams();
  if (categoryId) query.set('categoryId', categoryId);
  if (search) query.set('search', search);

  const response = await fetch(`${API_BASE}/public/faq?${query.toString()}`);
  if (!response.ok) {
    throw new Error('Failed to load FAQ items');
  }

  return response.json();
}

export async function submitContact(name: string, email: string, message: string): Promise<void> {
  const response = await fetch(`${API_BASE}/public/contact`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, email, message })
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }
}
