/**
 * Icon Registry - Centralized icon configuration for ClimaSite
 * 
 * This registry maps semantic icon names to Lucide icon names and provides
 * a single source of truth for all icons used in the application.
 * 
 * @see https://lucide.dev/icons for the full Lucide icon library
 */

// Import all commonly used icons from Lucide
import {
  // Navigation
  Menu,
  X,
  ChevronDown,
  ChevronUp,
  ChevronLeft,
  ChevronRight,
  ArrowLeft,
  ArrowRight,
  ArrowUp,
  ArrowDown,
  Home,
  ExternalLink,
  
  // Commerce
  ShoppingCart,
  ShoppingBag,
  Heart,
  CreditCard,
  Package,
  Truck,
  Tag,
  Percent,
  Receipt,
  Wallet,
  Banknote,
  Building2,
  
  // User
  User,
  UserPlus,
  UserMinus,
  UserCheck,
  Users,
  Settings,
  LogOut,
  LogIn,
  Bell,
  Mail,
  Phone,
  MapPin,
  
  // Actions
  Search,
  Filter,
  Plus,
  Minus,
  Trash2,
  Edit,
  Edit2,
  Edit3,
  Check,
  Copy,
  Download,
  Upload,
  Share2,
  RefreshCw,
  RotateCcw,
  Save,
  Printer,
  Eye,
  EyeOff,
  Lock,
  Unlock,
  
  // Status
  Info,
  AlertTriangle,
  AlertCircle,
  CheckCircle,
  CheckCircle2,
  XCircle,
  HelpCircle,
  Clock,
  Calendar,
  CalendarDays,
  
  // Social
  Facebook,
  Instagram,
  Twitter,
  Linkedin,
  Youtube,
  Github,
  
  // Media
  Image,
  Camera,
  Video,
  Play,
  Pause,
  Volume2,
  VolumeX,
  Maximize,
  Minimize,
  
  // Layout
  Grid,
  List,
  LayoutGrid,
  LayoutList,
  Columns,
  Rows,
  
  // Weather/HVAC specific
  Thermometer,
  ThermometerSun,
  ThermometerSnowflake,
  Wind,
  Snowflake,
  Sun,
  CloudSun,
  Droplets,
  Fan,
  Gauge,
  Zap,
  
  // Misc
  Star,
  StarHalf,
  Bookmark,
  Flag,
  Award,
  Gift,
  FileText,
  File,
  Folder,
  MessageSquare,
  MessageCircle,
  Send,
  Loader2,
  MoreHorizontal,
  MoreVertical,
  Grip,
  GripVertical,
  
  // Type for LucideIconData
  type LucideIconData
} from 'lucide-angular';

/**
 * All icons used in the application, organized by category.
 * Use LucideAngularModule.pick(ICON_REGISTRY) to register them.
 */
export const ICON_REGISTRY = {
  // Navigation
  Menu,
  X,
  ChevronDown,
  ChevronUp,
  ChevronLeft,
  ChevronRight,
  ArrowLeft,
  ArrowRight,
  ArrowUp,
  ArrowDown,
  Home,
  ExternalLink,
  
  // Commerce
  ShoppingCart,
  ShoppingBag,
  Heart,
  CreditCard,
  Package,
  Truck,
  Tag,
  Percent,
  Receipt,
  Wallet,
  Banknote,
  Building2,
  
  // User
  User,
  UserPlus,
  UserMinus,
  UserCheck,
  Users,
  Settings,
  LogOut,
  LogIn,
  Bell,
  Mail,
  Phone,
  MapPin,
  
  // Actions
  Search,
  Filter,
  Plus,
  Minus,
  Trash2,
  Edit,
  Edit2,
  Edit3,
  Check,
  Copy,
  Download,
  Upload,
  Share2,
  RefreshCw,
  RotateCcw,
  Save,
  Printer,
  Eye,
  EyeOff,
  Lock,
  Unlock,
  
  // Status
  Info,
  AlertTriangle,
  AlertCircle,
  CheckCircle,
  CheckCircle2,
  XCircle,
  HelpCircle,
  Clock,
  Calendar,
  CalendarDays,
  
  // Social
  Facebook,
  Instagram,
  Twitter,
  Linkedin,
  Youtube,
  Github,
  
  // Media
  Image,
  Camera,
  Video,
  Play,
  Pause,
  Volume2,
  VolumeX,
  Maximize,
  Minimize,
  
  // Layout
  Grid,
  List,
  LayoutGrid,
  LayoutList,
  Columns,
  Rows,
  
  // Weather/HVAC specific
  Thermometer,
  ThermometerSun,
  ThermometerSnowflake,
  Wind,
  Snowflake,
  Sun,
  CloudSun,
  Droplets,
  Fan,
  Gauge,
  Zap,
  
  // Misc
  Star,
  StarHalf,
  Bookmark,
  Flag,
  Award,
  Gift,
  FileText,
  File,
  Folder,
  MessageSquare,
  MessageCircle,
  Send,
  Loader2,
  MoreHorizontal,
  MoreVertical,
  Grip,
  GripVertical
};

/**
 * Icon category definitions for documentation and organization
 */
export const ICON_CATEGORIES = {
  navigation: [
    'menu', 'x', 'chevron-down', 'chevron-up', 'chevron-left', 'chevron-right',
    'arrow-left', 'arrow-right', 'arrow-up', 'arrow-down', 'home', 'external-link'
  ],
  commerce: [
    'shopping-cart', 'shopping-bag', 'heart', 'credit-card', 'package', 'truck',
    'tag', 'percent', 'receipt', 'wallet', 'banknote', 'building-2'
  ],
  user: [
    'user', 'user-plus', 'user-minus', 'user-check', 'users', 'settings',
    'log-out', 'log-in', 'bell', 'mail', 'phone', 'map-pin'
  ],
  actions: [
    'search', 'filter', 'plus', 'minus', 'trash-2', 'edit', 'edit-2', 'edit-3',
    'check', 'copy', 'download', 'upload', 'share-2', 'refresh-cw', 'rotate-ccw',
    'save', 'printer', 'eye', 'eye-off', 'lock', 'unlock'
  ],
  status: [
    'info', 'alert-triangle', 'alert-circle', 'check-circle', 'check-circle-2',
    'x-circle', 'help-circle', 'clock', 'calendar', 'calendar-days'
  ],
  social: [
    'facebook', 'instagram', 'twitter', 'linkedin', 'youtube', 'github'
  ],
  media: [
    'image', 'camera', 'video', 'play', 'pause', 'volume-2', 'volume-x',
    'maximize', 'minimize'
  ],
  layout: [
    'grid', 'list', 'layout-grid', 'layout-list', 'columns', 'rows'
  ],
  hvac: [
    'thermometer', 'thermometer-sun', 'thermometer-snowflake', 'wind', 'snowflake',
    'sun', 'cloud-sun', 'droplets', 'fan', 'gauge', 'zap'
  ],
  misc: [
    'star', 'star-half', 'bookmark', 'flag', 'award', 'gift', 'file-text', 'file',
    'folder', 'message-square', 'message-circle', 'send', 'loader-2',
    'more-horizontal', 'more-vertical', 'grip', 'grip-vertical'
  ]
} as const;

/**
 * Type for all registered icon names
 */
export type RegisteredIconName = keyof typeof ICON_REGISTRY;

/**
 * Helper to check if an icon is registered
 */
export function isIconRegistered(name: string): boolean {
  // Convert kebab-case to PascalCase for lookup
  const pascalName = name
    .split('-')
    .map(part => part.charAt(0).toUpperCase() + part.slice(1))
    .join('');
  
  return pascalName in ICON_REGISTRY;
}

/**
 * Export the LucideIconData type for external use
 */
export type { LucideIconData };
