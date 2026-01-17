export interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string;
  icon?: string;
  imageUrl?: string;
  sortOrder: number;
  isActive: boolean;
  parentId?: string;
  parentCategory?: {
    name: string;
    slug: string;
  };
  children: Category[];
  productCount: number;
}

export interface CategoryTree {
  id: string;
  name: string;
  slug: string;
  imageUrl?: string;
  children: CategoryTree[];
}
