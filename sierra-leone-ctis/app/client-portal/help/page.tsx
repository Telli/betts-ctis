'use client'

import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from '@/components/ui/accordion'
import {
  Search,
  HelpCircle,
  MessageSquare,
  Phone,
  Mail,
  FileText,
  BookOpen,
  ExternalLink,
  ChevronRight
} from 'lucide-react'

interface FAQItem {
  id: string
  question: string
  answer: string
  category: string
}

const faqData: FAQItem[] = [
  {
    id: '1',
    question: 'How do I upload documents?',
    answer: 'To upload documents, navigate to the Documents section in your client portal. Click the "Upload Document" button, select your file, choose the document type, and add any relevant notes. Your documents will be securely stored and accessible to your tax advisor.',
    category: 'Documents'
  },
  {
    id: '2',
    question: 'How do I view my tax filing status?',
    answer: 'Go to the Tax Filings section to see all your current and past tax filings. Each filing shows its status (Draft, Submitted, Approved, etc.) and includes details about deadlines and requirements.',
    category: 'Tax Filings'
  },
  {
    id: '3',
    question: 'How do I make a payment?',
    answer: 'Navigate to the Payments section and click "Make Payment". Select the payment type, enter the amount, and choose your preferred payment method. You can also view your payment history and download receipts.',
    category: 'Payments'
  },
  {
    id: '4',
    question: 'What are compliance deadlines?',
    answer: 'Compliance deadlines are important dates for tax filings, payments, and regulatory requirements. Check the Deadlines section for a calendar view of all upcoming deadlines, or visit the Compliance section for detailed status information.',
    category: 'Compliance'
  },
  {
    id: '5',
    question: 'How do I communicate with my tax advisor?',
    answer: 'Use the Messages section to send secure messages to your tax advisor. You can attach documents, ask questions, and receive responses. All communications are encrypted and stored for your reference.',
    category: 'Communication'
  },
  {
    id: '6',
    question: 'How do I update my profile information?',
    answer: 'Go to the Profile section to update your personal information, contact details, and preferences. You can also change your password and manage notification settings from the Settings page.',
    category: 'Account'
  },
  {
    id: '7',
    question: 'What documents do I need to provide?',
    answer: 'Required documents vary by your tax situation, but commonly include income statements, expense records, bank statements, and identification documents. Your tax advisor will specify exactly what\'s needed for your filings.',
    category: 'Documents'
  },
  {
    id: '8',
    question: 'How do I view reports?',
    answer: 'Access the Reports section to view various tax-related reports including income summaries, expense breakdowns, and compliance status reports. You can filter by date range and export reports as needed.',
    category: 'Reports'
  }
]

const categories = ['All', 'Documents', 'Tax Filings', 'Payments', 'Compliance', 'Communication', 'Account', 'Reports']

export default function ClientHelpPage() {
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedCategory, setSelectedCategory] = useState('All')

  const filteredFAQs = faqData.filter(faq => {
    const matchesSearch = faq.question.toLowerCase().includes(searchQuery.toLowerCase()) ||
                         faq.answer.toLowerCase().includes(searchQuery.toLowerCase())
    const matchesCategory = selectedCategory === 'All' || faq.category === selectedCategory
    return matchesSearch && matchesCategory
  })

  return (
    <div className="flex-1 space-y-4 p-4 md:p-8 pt-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">Help & Support</h2>
          <p className="text-muted-foreground">
            Find answers to common questions and get support
          </p>
        </div>
      </div>

      <div className="grid gap-6">
        {/* Search and Quick Actions */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <HelpCircle className="h-5 w-5" />
              How can we help you?
            </CardTitle>
            <CardDescription>
              Search our knowledge base or contact support
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="relative">
              <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search FAQs..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-9"
              />
            </div>

            <div className="flex flex-wrap gap-2">
              {categories.map((category) => (
                <Button
                  key={category}
                  variant={selectedCategory === category ? "default" : "outline"}
                  size="sm"
                  onClick={() => setSelectedCategory(category)}
                >
                  {category}
                </Button>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* FAQ Section */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <BookOpen className="h-5 w-5" />
              Frequently Asked Questions
            </CardTitle>
            <CardDescription>
              {filteredFAQs.length} {filteredFAQs.length === 1 ? 'result' : 'results'} found
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Accordion type="single" collapsible className="w-full">
              {filteredFAQs.map((faq) => (
                <AccordionItem key={faq.id} value={faq.id}>
                  <AccordionTrigger className="text-left">
                    <div className="flex items-center gap-2">
                      <Badge variant="secondary" className="text-xs">
                        {faq.category}
                      </Badge>
                      {faq.question}
                    </div>
                  </AccordionTrigger>
                  <AccordionContent className="text-muted-foreground">
                    {faq.answer}
                  </AccordionContent>
                </AccordionItem>
              ))}
            </Accordion>

            {filteredFAQs.length === 0 && (
              <div className="text-center py-8">
                <HelpCircle className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                <h3 className="text-lg font-medium mb-2">No results found</h3>
                <p className="text-muted-foreground">
                  Try adjusting your search terms or browse all categories.
                </p>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Contact Support */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <MessageSquare className="h-5 w-5" />
              Contact Support
            </CardTitle>
            <CardDescription>
              Can't find what you're looking for? Get in touch with our support team
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-4 md:grid-cols-3">
              <div className="flex items-center gap-3 p-4 border rounded-lg">
                <Mail className="h-8 w-8 text-blue-500" />
                <div>
                  <h4 className="font-medium">Email Support</h4>
                  <p className="text-sm text-muted-foreground">support@bettstax.com</p>
                  <p className="text-xs text-muted-foreground">Response within 24 hours</p>
                </div>
              </div>

              <div className="flex items-center gap-3 p-4 border rounded-lg">
                <Phone className="h-8 w-8 text-green-500" />
                <div>
                  <h4 className="font-medium">Phone Support</h4>
                  <p className="text-sm text-muted-foreground">+232 22 123 456</p>
                  <p className="text-xs text-muted-foreground">Mon-Fri, 9AM-5PM GMT</p>
                </div>
              </div>

              <div className="flex items-center gap-3 p-4 border rounded-lg">
                <MessageSquare className="h-8 w-8 text-purple-500" />
                <div>
                  <h4 className="font-medium">Live Chat</h4>
                  <p className="text-sm text-muted-foreground">Available now</p>
                  <p className="text-xs text-muted-foreground">Average wait: 2 minutes</p>
                </div>
              </div>
            </div>

            <Separator />

            <div className="flex gap-2">
              <Button className="flex-1">
                <MessageSquare className="h-4 w-4 mr-2" />
                Start Live Chat
              </Button>
              <Button variant="outline" className="flex-1">
                <Mail className="h-4 w-4 mr-2" />
                Send Email
              </Button>
            </div>
          </CardContent>
        </Card>

        {/* Resources */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <FileText className="h-5 w-5" />
              Additional Resources
            </CardTitle>
            <CardDescription>
              Helpful guides and documentation
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="flex items-center justify-between p-4 border rounded-lg hover:bg-muted/50 cursor-pointer">
                <div className="flex items-center gap-3">
                  <BookOpen className="h-5 w-5 text-blue-500" />
                  <div>
                    <h4 className="font-medium">User Guide</h4>
                    <p className="text-sm text-muted-foreground">Complete guide to using the portal</p>
                  </div>
                </div>
                <ExternalLink className="h-4 w-4 text-muted-foreground" />
              </div>

              <div className="flex items-center justify-between p-4 border rounded-lg hover:bg-muted/50 cursor-pointer">
                <div className="flex items-center gap-3">
                  <FileText className="h-5 w-5 text-green-500" />
                  <div>
                    <h4 className="font-medium">Tax Filing Guide</h4>
                    <p className="text-sm text-muted-foreground">Step-by-step filing instructions</p>
                  </div>
                </div>
                <ExternalLink className="h-4 w-4 text-muted-foreground" />
              </div>

              <div className="flex items-center justify-between p-4 border rounded-lg hover:bg-muted/50 cursor-pointer">
                <div className="flex items-center gap-3">
                  <FileText className="h-5 w-5 text-purple-500" />
                  <div>
                    <h4 className="font-medium">Compliance Checklist</h4>
                    <p className="text-sm text-muted-foreground">Ensure you're meeting all requirements</p>
                  </div>
                </div>
                <ExternalLink className="h-4 w-4 text-muted-foreground" />
              </div>

              <div className="flex items-center justify-between p-4 border rounded-lg hover:bg-muted/50 cursor-pointer">
                <div className="flex items-center gap-3">
                  <FileText className="h-5 w-5 text-orange-500" />
                  <div>
                    <h4 className="font-medium">Video Tutorials</h4>
                    <p className="text-sm text-muted-foreground">Watch and learn with our video guides</p>
                  </div>
                </div>
                <ExternalLink className="h-4 w-4 text-muted-foreground" />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}