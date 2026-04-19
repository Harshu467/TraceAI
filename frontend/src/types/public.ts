export type SeoMetadata = {
  title: string;
  description: string;
  ogImageUrl: string;
};

export type PublicPage = {
  slug: string;
  title: string;
  body: string;
  seo: SeoMetadata;
};

export type FaqCategory = {
  id: string;
  name: string;
};

export type FaqItem = {
  id: string;
  question: string;
  answer: string;
  category: FaqCategory;
};
