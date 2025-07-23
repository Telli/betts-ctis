'use client'

import React, { useState, useMemo } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { 
  Search, 
  Filter, 
  ArrowUpDown, 
  ArrowUp, 
  ArrowDown,
  ChevronLeft,
  ChevronRight,
  MoreHorizontal,
  Download,
  RefreshCw
} from 'lucide-react'
import { cn } from '@/lib/utils'

export interface Column<T> {
  key: keyof T
  label: string
  sortable?: boolean
  filterable?: boolean
  render?: (value: any, item: T) => React.ReactNode
  className?: string
  width?: string
}

export interface FilterOption {
  value: string
  label: string
  count?: number
}

export interface AdvancedDataTableProps<T> {
  data: T[]
  columns: Column<T>[]
  searchable?: boolean
  searchPlaceholder?: string
  filters?: Record<string, FilterOption[]>
  defaultSort?: { column: keyof T; direction: 'asc' | 'desc' }
  pageSize?: number
  loading?: boolean
  error?: string | null
  title?: string
  description?: string
  onRefresh?: () => void
  onExport?: () => void
  actions?: React.ReactNode
  rowActions?: (item: T) => React.ReactNode
  className?: string
  emptyStateMessage?: string
  emptyStateAction?: React.ReactNode
}

type SortDirection = 'asc' | 'desc' | null

export function AdvancedDataTable<T extends Record<string, any>>({
  data,
  columns,
  searchable = true,
  searchPlaceholder = 'Search...',
  filters = {},
  defaultSort,
  pageSize = 10,
  loading = false,
  error = null,
  title,
  description,
  onRefresh,
  onExport,
  actions,
  rowActions,
  className,
  emptyStateMessage = 'No data found',
  emptyStateAction
}: AdvancedDataTableProps<T>) {
  const [searchTerm, setSearchTerm] = useState('')
  const [activeFilters, setActiveFilters] = useState<Record<string, string>>({})
  const [sortColumn, setSortColumn] = useState<keyof T | null>(defaultSort?.column || null)
  const [sortDirection, setSortDirection] = useState<SortDirection>(defaultSort?.direction || null)
  const [currentPage, setCurrentPage] = useState(1)

  // Filter and sort data
  const filteredAndSortedData = useMemo(() => {
    let result = [...data]

    // Apply text search
    if (searchable && searchTerm.trim()) {
      const searchLower = searchTerm.toLowerCase()
      result = result.filter(item =>
        columns.some(column => {
          const value = item[column.key]
          return value && String(value).toLowerCase().includes(searchLower)
        })
      )
    }

    // Apply filters
    Object.entries(activeFilters).forEach(([filterKey, filterValue]) => {
      if (filterValue && filterValue !== 'all') {
        result = result.filter(item => String(item[filterKey]) === filterValue)
      }
    })

    // Apply sorting
    if (sortColumn && sortDirection) {
      result.sort((a, b) => {
        const aValue = a[sortColumn]
        const bValue = b[sortColumn]

        // Handle null/undefined values
        if (aValue == null && bValue == null) return 0
        if (aValue == null) return sortDirection === 'asc' ? -1 : 1
        if (bValue == null) return sortDirection === 'asc' ? 1 : -1

        // Handle different data types
        if (typeof aValue === 'number' && typeof bValue === 'number') {
          return sortDirection === 'asc' ? aValue - bValue : bValue - aValue
        }

        if (aValue && bValue && Object.prototype.toString.call(aValue) === '[object Date]' && Object.prototype.toString.call(bValue) === '[object Date]') {
          const dateA = aValue as Date
          const dateB = bValue as Date
          return sortDirection === 'asc' 
            ? dateA.getTime() - dateB.getTime()
            : dateB.getTime() - dateA.getTime()
        }

        // Default string comparison
        const aStr = String(aValue).toLowerCase()
        const bStr = String(bValue).toLowerCase()
        
        if (aStr < bStr) return sortDirection === 'asc' ? -1 : 1
        if (aStr > bStr) return sortDirection === 'asc' ? 1 : -1
        return 0
      })
    }

    return result
  }, [data, searchTerm, activeFilters, sortColumn, sortDirection, columns, searchable])

  // Pagination
  const totalPages = Math.ceil(filteredAndSortedData.length / pageSize)
  const paginatedData = filteredAndSortedData.slice(
    (currentPage - 1) * pageSize,
    currentPage * pageSize
  )

  // Handle sorting
  const handleSort = (column: keyof T) => {
    if (!columns.find(col => col.key === column)?.sortable) return

    if (sortColumn === column) {
      if (sortDirection === 'asc') {
        setSortDirection('desc')
      } else if (sortDirection === 'desc') {
        setSortColumn(null)
        setSortDirection(null)
      } else {
        setSortDirection('asc')
      }
    } else {
      setSortColumn(column)
      setSortDirection('asc')
    }
  }

  // Handle filter change
  const handleFilterChange = (filterKey: string, value: string) => {
    setActiveFilters(prev => ({
      ...prev,
      [filterKey]: value
    }))
    setCurrentPage(1) // Reset to first page when filtering
  }

  // Clear all filters
  const clearFilters = () => {
    setSearchTerm('')
    setActiveFilters({})
    setSortColumn(null)
    setSortDirection(null)
    setCurrentPage(1)
  }

  const getSortIcon = (column: keyof T) => {
    if (!columns.find(col => col.key === column)?.sortable) return null
    
    if (sortColumn === column) {
      return sortDirection === 'asc' 
        ? <ArrowUp className="h-4 w-4 text-sierra-blue-600" />
        : <ArrowDown className="h-4 w-4 text-sierra-blue-600" />
    }
    return <ArrowUpDown className="h-4 w-4 text-gray-400" />
  }

  // Count active filters
  const activeFilterCount = Object.values(activeFilters).filter(v => v && v !== 'all').length + 
                           (searchTerm ? 1 : 0)

  if (loading) {
    return (
      <Card className={className}>
        <CardContent className="p-8">
          <div className="flex items-center justify-center">
            <RefreshCw className="h-8 w-8 animate-spin text-sierra-blue-600" />
            <span className="ml-2 text-gray-600">Loading...</span>
          </div>
        </CardContent>
      </Card>
    )
  }

  if (error) {
    return (
      <Card className={className}>
        <CardContent className="p-8">
          <div className="text-center">
            <div className="text-red-600 mb-4">{error}</div>
            {onRefresh && (
              <Button onClick={onRefresh} variant="outline">
                <RefreshCw className="h-4 w-4 mr-2" />
                Retry
              </Button>
            )}
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className={className}>
      {(title || description || actions) && (
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              {title && <CardTitle>{title}</CardTitle>}
              {description && <CardDescription>{description}</CardDescription>}
            </div>
            <div className="flex items-center space-x-2">
              {onRefresh && (
                <Button variant="outline" size="sm" onClick={onRefresh}>
                  <RefreshCw className="h-4 w-4" />
                </Button>
              )}
              {onExport && (
                <Button variant="outline" size="sm" onClick={onExport}>
                  <Download className="h-4 w-4 mr-2" />
                  Export
                </Button>
              )}
              {actions}
            </div>
          </div>
        </CardHeader>
      )}
      
      <CardContent className="p-0">
        {/* Search and Filters */}
        {(searchable || Object.keys(filters).length > 0) && (
          <div className="p-6 border-b bg-gray-50/50">
            <div className="flex flex-col sm:flex-row gap-4">
              {/* Search */}
              {searchable && (
                <div className="flex-1">
                  <div className="relative">
                    <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                    <Input
                      placeholder={searchPlaceholder}
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                      className="pl-10"
                    />
                  </div>
                </div>
              )}

              {/* Filters */}
              {Object.entries(filters).map(([filterKey, options]) => (
                <Select
                  key={filterKey}
                  value={activeFilters[filterKey] || 'all'}
                  onValueChange={(value) => handleFilterChange(filterKey, value)}
                >
                  <SelectTrigger className="w-full sm:w-48">
                    <SelectValue placeholder={`Filter by ${filterKey}`} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All {filterKey}s</SelectItem>
                    {options.map(option => (
                      <SelectItem key={option.value} value={option.value}>
                        <div className="flex items-center justify-between w-full">
                          <span>{option.label}</span>
                          {option.count !== undefined && (
                            <Badge variant="secondary" className="ml-2">
                              {option.count}
                            </Badge>
                          )}
                        </div>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              ))}

              {/* Clear filters */}
              {activeFilterCount > 0 && (
                <Button variant="outline" onClick={clearFilters}>
                  Clear {activeFilterCount > 1 ? `(${activeFilterCount})` : ''}
                </Button>
              )}
            </div>

            {/* Active filters display */}
            {activeFilterCount > 0 && (
              <div className="mt-3 flex flex-wrap gap-2">
                {searchTerm && (
                  <Badge variant="secondary">
                    Search: "{searchTerm}"
                    <button
                      onClick={() => setSearchTerm('')}
                      className="ml-1 hover:text-red-600"
                    >
                      ×
                    </button>
                  </Badge>
                )}
                {Object.entries(activeFilters).map(([key, value]) => {
                  if (!value || value === 'all') return null
                  const option = filters[key]?.find(opt => opt.value === value)
                  return (
                    <Badge key={key} variant="secondary">
                      {key}: {option?.label || value}
                      <button
                        onClick={() => handleFilterChange(key, 'all')}
                        className="ml-1 hover:text-red-600"
                      >
                        ×
                      </button>
                    </Badge>
                  )
                })}
              </div>
            )}
          </div>
        )}

        {/* Data Table */}
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50 border-b">
              <tr>
                {columns.map((column) => (
                  <th
                    key={String(column.key)}
                    className={cn(
                      "text-left p-4 font-semibold text-gray-900",
                      column.sortable && "cursor-pointer hover:bg-gray-100",
                      column.className
                    )}
                    style={column.width ? { width: column.width } : undefined}
                    onClick={() => column.sortable && handleSort(column.key)}
                  >
                    <div className="flex items-center space-x-1">
                      <span>{column.label}</span>
                      {getSortIcon(column.key)}
                    </div>
                  </th>
                ))}
                {rowActions && (
                  <th className="text-left p-4 font-semibold text-gray-900">Actions</th>
                )}
              </tr>
            </thead>
            <tbody>
              {paginatedData.length === 0 ? (
                <tr>
                  <td colSpan={columns.length + (rowActions ? 1 : 0)} className="text-center py-12">
                    <div className="text-gray-500">
                      <div className="text-lg font-medium mb-2">{emptyStateMessage}</div>
                      <div className="text-sm mb-4">
                        {filteredAndSortedData.length === 0 && data.length > 0 
                          ? 'Try adjusting your search or filters'
                          : 'No data available'
                        }
                      </div>
                      {emptyStateAction}
                    </div>
                  </td>
                </tr>
              ) : (
                paginatedData.map((item, index) => (
                  <tr 
                    key={index} 
                    className="border-b hover:bg-gray-50 transition-colors"
                  >
                    {columns.map((column) => (
                      <td 
                        key={String(column.key)} 
                        className={cn("p-4", column.className)}
                      >
                        {column.render 
                          ? column.render(item[column.key], item)
                          : String(item[column.key] || '')
                        }
                      </td>
                    ))}
                    {rowActions && (
                      <td className="p-4">
                        {rowActions(item)}
                      </td>
                    )}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between p-4 border-t">
            <div className="text-sm text-gray-500">
              Showing {(currentPage - 1) * pageSize + 1} to {Math.min(currentPage * pageSize, filteredAndSortedData.length)} of {filteredAndSortedData.length} results
            </div>
            <div className="flex items-center space-x-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                disabled={currentPage === 1}
              >
                <ChevronLeft className="h-4 w-4 mr-1" />
                Previous
              </Button>
              
              <div className="flex space-x-1">
                {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                  const pageNum = currentPage <= 3 
                    ? i + 1 
                    : currentPage >= totalPages - 2
                      ? totalPages - 4 + i
                      : currentPage - 2 + i
                      
                  if (pageNum < 1 || pageNum > totalPages) return null
                  
                  return (
                    <Button
                      key={pageNum}
                      variant={currentPage === pageNum ? 'default' : 'outline'}
                      size="sm"
                      onClick={() => setCurrentPage(pageNum)}
                      className={currentPage === pageNum ? 'bg-sierra-blue-600 hover:bg-sierra-blue-700' : ''}
                    >
                      {pageNum}
                    </Button>
                  )
                })}
              </div>
              
              <Button
                variant="outline"
                size="sm"
                onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
                disabled={currentPage === totalPages}
              >
                Next
                <ChevronRight className="h-4 w-4 ml-1" />
              </Button>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}

export default AdvancedDataTable