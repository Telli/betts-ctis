"use client"

import { useRef, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { useToast } from '@/components/ui/use-toast';
import { AlertCircle, Upload, Plus, Trash2 } from 'lucide-react';
import type { TaxFilingDto } from '@/lib/services';

export interface SchedulesTabProps {
  filing?: TaxFilingDto;
  mode?: 'create' | 'edit' | 'view';
}

interface ScheduleItem {
  id: number;
  description: string;
  amount: number;
  taxable: number;
}

export function SchedulesTab({ filing, mode = 'edit' }: SchedulesTabProps) {
  const isReadOnly = mode === 'view';
  const { toast } = useToast();
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  
  // Mock data - in real implementation, this would come from API
  const [scheduleData, setScheduleData] = useState<ScheduleItem[]>([
    { id: 1, description: 'Sales Revenue', amount: 250000, taxable: 250000 },
    { id: 2, description: 'Cost of Goods Sold', amount: 150000, taxable: 0 },
    { id: 3, description: 'Operating Expenses', amount: 50000, taxable: 0 },
  ]);

  const handleAddRow = () => {
    const newId = Math.max(...scheduleData.map(s => s.id), 0) + 1;
    setScheduleData([...scheduleData, { id: newId, description: '', amount: 0, taxable: 0 }]);
  };

  const handleDeleteRow = (id: number) => {
    setScheduleData(scheduleData.filter(item => item.id !== id));
  };

  const parseCsv = (text: string): Array<{ description: string; amount: number; taxable: number }> => {
    const rows: Array<{ description: string; amount: number; taxable: number }> = [];
    const lines = text.split(/\r?\n/).filter(l => l.trim().length > 0);
    if (lines.length === 0) return rows;
    // detect header: look for keywords
    const header = lines[0].toLowerCase();
    const hasHeader = header.includes('description') || header.includes('amount');
    const startIdx = hasHeader ? 1 : 0;

    const splitCsvLine = (line: string): string[] => {
      const result: string[] = [];
      let current = '';
      let inQuotes = false;
      for (let i = 0; i < line.length; i++) {
        const ch = line[i];
        if (ch === '"') {
          if (inQuotes && line[i + 1] === '"') {
            current += '"';
            i++;
          } else {
            inQuotes = !inQuotes;
          }
        } else if (ch === ',' && !inQuotes) {
          result.push(current);
          current = '';
        } else {
          current += ch;
        }
      }
      result.push(current);
      return result.map(s => s.trim());
    };

    for (let i = startIdx; i < lines.length; i++) {
      const cols = splitCsvLine(lines[i]);
      if (cols.length < 3) continue;
      const description = cols[0] || '';
      const amount = parseFloat(cols[1].replace(/[^0-9.-]/g, '')) || 0;
      const taxable = parseFloat(cols[2].replace(/[^0-9.-]/g, '')) || 0;
      rows.push({ description, amount, taxable });
    }
    return rows;
  };

  const handleFileSelected = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    try {
      const text = await file.text();
      const rows = parseCsv(text);
      if (!rows.length) {
        toast({ variant: 'destructive', title: 'Import failed', description: 'No valid rows found in CSV.' });
        return;
      }
      const newRows: ScheduleItem[] = rows.map((r, idx) => ({
        id: Math.max(...scheduleData.map(s => s.id), 0) + idx + 1,
        description: r.description,
        amount: r.amount,
        taxable: r.taxable,
      }));
      setScheduleData(prev => [...prev, ...newRows]);
      toast({ title: 'Import complete', description: `${newRows.length} row(s) added from CSV.` });
    } catch (err) {
      console.error('CSV import error', err);
      toast({ variant: 'destructive', title: 'Import failed', description: 'Could not parse the CSV file.' });
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const handleImport = () => {
    if (isReadOnly) return;
    fileInputRef.current?.click();
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle>Schedule Data</CardTitle>
          <div className="flex gap-2">
            {!isReadOnly && (
              <>
                <Button variant="outline" size="sm" onClick={handleAddRow}>
                  <Plus className="w-4 h-4 mr-2" />
                  Add Row
                </Button>
                <Button variant="outline" size="sm" onClick={handleImport}>
                  <Upload className="w-4 h-4 mr-2" />
                  Import CSV/Excel
                </Button>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".csv"
                  className="hidden"
                  onChange={handleFileSelected}
                />
              </>
            )}
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <Alert className="mb-6">
          <AlertCircle className="w-4 h-4" />
          <AlertDescription>
            Upload a CSV or Excel file with columns: Description, Amount, Taxable Amount
          </AlertDescription>
        </Alert>

        <div className="border border-gray-200 rounded-lg overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-[50%]">Description</TableHead>
                <TableHead className="text-right">Amount (SLE)</TableHead>
                <TableHead className="text-right">Taxable (SLE)</TableHead>
                {!isReadOnly && <TableHead className="w-[100px]">Actions</TableHead>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {scheduleData.map((row) => (
                <TableRow key={row.id}>
                  <TableCell>
                    {isReadOnly ? (
                      row.description
                    ) : (
                      <input
                        type="text"
                        value={row.description}
                        onChange={(e) => {
                          setScheduleData(scheduleData.map(item => 
                            item.id === row.id ? { ...item, description: e.target.value } : item
                          ));
                        }}
                        className="w-full px-2 py-1 border border-gray-300 rounded"
                        placeholder="Enter description..."
                      />
                    )}
                  </TableCell>
                  <TableCell className="text-right font-mono">
                    {isReadOnly ? (
                      row.amount.toLocaleString()
                    ) : (
                      <input
                        type="number"
                        value={row.amount}
                        onChange={(e) => {
                          setScheduleData(scheduleData.map(item => 
                            item.id === row.id ? { ...item, amount: parseFloat(e.target.value) || 0 } : item
                          ));
                        }}
                        className="w-full px-2 py-1 border border-gray-300 rounded text-right"
                      />
                    )}
                  </TableCell>
                  <TableCell className="text-right font-mono">
                    {isReadOnly ? (
                      row.taxable.toLocaleString()
                    ) : (
                      <input
                        type="number"
                        value={row.taxable}
                        onChange={(e) => {
                          setScheduleData(scheduleData.map(item => 
                            item.id === row.id ? { ...item, taxable: parseFloat(e.target.value) || 0 } : item
                          ));
                        }}
                        className="w-full px-2 py-1 border border-gray-300 rounded text-right"
                      />
                    )}
                  </TableCell>
                  {!isReadOnly && (
                    <TableCell>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleDeleteRow(row.id)}
                      >
                        <Trash2 className="w-4 h-4 text-red-600" />
                      </Button>
                    </TableCell>
                  )}
                </TableRow>
              ))}
              {scheduleData.length === 0 && (
                <TableRow>
                  <TableCell colSpan={isReadOnly ? 3 : 4} className="text-center text-gray-500 py-8">
                    No schedule data. Click "Add Row" or "Import CSV/Excel" to get started.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>

        {/* Summary Row */}
        <div className="mt-4 p-4 bg-gray-50 rounded-lg border border-gray-200">
          <div className="flex justify-between items-center">
            <span className="font-semibold">Total:</span>
            <div className="flex gap-8">
              <div className="text-right">
                <div className="text-xs text-gray-600">Amount</div>
                <div className="font-semibold">
                  SLE {scheduleData.reduce((sum, item) => sum + item.amount, 0).toLocaleString()}
                </div>
              </div>
              <div className="text-right">
                <div className="text-xs text-gray-600">Taxable</div>
                <div className="font-semibold">
                  SLE {scheduleData.reduce((sum, item) => sum + item.taxable, 0).toLocaleString()}
                </div>
              </div>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
