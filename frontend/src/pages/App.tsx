import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import { getFaqCategories, getFaqItems, getPublicPage, submitContact } from '../services/publicApi';
import type { FaqCategory, FaqItem, PublicPage } from '../types/public';
import { WorkspacePage } from './WorkspacePage';

function useSeo(page?: PublicPage) {
  useEffect(() => {
    if (!page) return;
    document.title = page.seo.title;

    const description = document.querySelector('meta[name="description"]') ?? document.createElement('meta');
    description.setAttribute('name', 'description');
    description.setAttribute('content', page.seo.description);
    document.head.appendChild(description);

    const ogImage = document.querySelector('meta[property="og:image"]') ?? document.createElement('meta');
    ogImage.setAttribute('property', 'og:image');
    ogImage.setAttribute('content', page.seo.ogImageUrl);
    document.head.appendChild(ogImage);
  }, [page]);
}

function PublicPageView({ slug }: { slug: string }) {
  const [page, setPage] = useState<PublicPage>();

  useEffect(() => {
    getPublicPage(slug).then(setPage).catch(() => setPage(undefined));
  }, [slug]);

  useSeo(page);

  if (!page) return <section className="card">Loading...</section>;
  return (
    <section className="card">
      <h1>{page.title}</h1>
      <p>{page.body}</p>
      {slug === '' && (
        <div className="cta-row">
          <a className="button-link" href="/login">Login</a>
          <a className="button-link" href="/signup">Sign Up</a>
        </div>
      )}
    </section>
  );
}

function FaqPage() {
  const [categories, setCategories] = useState<FaqCategory[]>([]);
  const [selectedCategory, setSelectedCategory] = useState('');
  const [search, setSearch] = useState('');
  const [items, setItems] = useState<FaqItem[]>([]);

  useEffect(() => {
    getFaqCategories().then(setCategories);
  }, []);

  useEffect(() => {
    getFaqItems(selectedCategory || undefined, search || undefined).then(setItems);
  }, [selectedCategory, search]);

  const grouped = useMemo(() => {
    return items.reduce<Record<string, FaqItem[]>>((acc, item) => {
      const key = item.category.name;
      acc[key] = acc[key] ?? [];
      acc[key].push(item);
      return acc;
    }, {});
  }, [items]);

  return (
    <section className="card">
      <h1>FAQ</h1>
      <div className="row">
        <input placeholder="Search FAQs" value={search} onChange={(e) => setSearch(e.target.value)} />
        <select value={selectedCategory} onChange={(e) => setSelectedCategory(e.target.value)}>
          <option value="">All categories</option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </select>
      </div>
      {Object.entries(grouped).map(([categoryName, categoryItems]) => (
        <div key={categoryName}>
          <h3>{categoryName}</h3>
          {categoryItems.map((item) => (
            <article className="faq-item" key={item.id}>
              <strong>{item.question}</strong>
              <p>{item.answer}</p>
            </article>
          ))}
        </div>
      ))}
    </section>
  );
}

function ContactPage() {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [message, setMessage] = useState('');
  const [status, setStatus] = useState('');

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    try {
      await submitContact(name, email, message);
      setStatus('Submitted successfully. Our support team has been notified.');
      setName('');
      setEmail('');
      setMessage('');
    } catch (error) {
      setStatus((error as Error).message);
    }
  };

  return (
    <section className="card">
      <h1>Contact</h1>
      <form onSubmit={onSubmit}>
        <label>Name</label>
        <input required value={name} onChange={(event) => setName(event.target.value)} />
        <label>Email</label>
        <input required type="email" value={email} onChange={(event) => setEmail(event.target.value)} />
        <label>Message</label>
        <textarea required minLength={10} rows={6} value={message} onChange={(event) => setMessage(event.target.value)} />
        <button type="submit">Send</button>
      </form>
      {status && <p className="muted">{status}</p>}
    </section>
  );
}

export function App() {
  const location = useLocation();
  const isPublicRoute = location.pathname !== '/app';

  return (
    <main className="layout">
      <header className="page-header">
        <h1>TraceAI</h1>
        <p>Trace-first AI execution workspace.</p>
        {isPublicRoute && (
          <div className="cta-row">
            <a className="button-link" href="/login">Login</a>
            <a className="button-link" href="/signup">Sign Up</a>
          </div>
        )}
      </header>

      <nav className="nav-grid" aria-label="Primary navigation">
        <Link className="nav-link" to="/">Home</Link>
        <Link className="nav-link" to="/how-it-works">How It Works</Link>
        <Link className="nav-link" to="/faq">FAQ</Link>
        <Link className="nav-link" to="/contact">Contact</Link>
        <Link className="nav-link" to="/app">Workspace</Link>
      </nav>

      <Routes>
        <Route path="/" element={<PublicPageView slug="" />} />
        <Route path="/how-it-works" element={<PublicPageView slug="how-it-works" />} />
        <Route path="/faq" element={<FaqPage />} />
        <Route path="/contact" element={<ContactPage />} />
        <Route path="/app" element={<WorkspacePage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </main>
  );
}
