import { ChevronRight } from 'lucide-react';
import Link from 'next/link';

export interface Breadcrumb {
  label: string;
  href?: string;
}

export interface PageHeaderProps {
  title: string;
  breadcrumbs?: Breadcrumb[];
  actions?: React.ReactNode;
  description?: string;
}

export function PageHeader({ title, breadcrumbs, actions, description }: PageHeaderProps) {
  return (
    <div className="border-b border-gray-200 bg-white px-6 py-4">
      {breadcrumbs && breadcrumbs.length > 0 && (
        <nav className="flex items-center space-x-2 text-sm mb-2" aria-label="Breadcrumb">
          {breadcrumbs.map((crumb, index) => (
            <div key={index} className="flex items-center">
              {index > 0 && <ChevronRight className="w-4 h-4 text-gray-400 mx-1" aria-hidden="true" />}
              {crumb.href ? (
                <Link
                  href={crumb.href}
                  className="text-sierra-blue-600 hover:text-sierra-blue-700 hover:underline transition-colors"
                >
                  {crumb.label}
                </Link>
              ) : (
                <span className="text-gray-600 font-medium" aria-current="page">
                  {crumb.label}
                </span>
              )}
            </div>
          ))}
        </nav>
      )}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-sierra-blue-900">{title}</h1>
          {description && (
            <p className="text-sm text-gray-600 mt-1">{description}</p>
          )}
        </div>
        {actions && <div className="flex items-center gap-2">{actions}</div>}
      </div>
    </div>
  );
}
