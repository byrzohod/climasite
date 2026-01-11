export interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string;
  imageUrl?: string;
  sortOrder: number;
  isActive: boolean;
  parentId?: string;
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
