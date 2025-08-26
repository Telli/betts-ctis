"use client"

import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { 
  CreditCard, 
  Smartphone, 
  Building2, 
  Globe, 
  CheckCircle, 
  AlertTriangle, 
  Clock,
  Info,
  Zap,
  Shield,
  TrendingUp
} from 'lucide-react'
import { cn } from '@/lib/utils'

export enum PaymentMethod {
  OrangeMoney = 'orange-money',
  AfricellMoney = 'africell-money',
  BankTransfer = 'bank-transfer',
  PayPal = 'paypal',
  Stripe = 'stripe'
}

interface PaymentProvider {
  id: PaymentMethod
  name: string
  displayName: string
  icon: React.ComponentType<{ className?: string }>
  description: string
  processingTime: string
  availability: 'online' | 'offline' | 'limited' | 'checking'
  fees: {
    fixed?: number
    percentage?: number
    minimum?: number
    maximum?: number
    ranges?: Array<{
      min: number
      max: number
      fee: number
    }>
  }
  features: string[]
  supportedCurrencies: string[]
  limits: {
    min: number
    max: number
    daily?: number
  }
  popularity: number // 1-5 stars
  securityLevel: 'high' | 'medium' | 'basic'
  isRecommended?: boolean
}

const paymentProviders: PaymentProvider[] = [
  {
    id: PaymentMethod.OrangeMoney,
    name: 'Orange Money',
    displayName: 'Orange Money',
    icon: Smartphone,
    description: 'Secure mobile money payments across Sierra Leone',
    processingTime: 'Instant',
    availability: 'online',
    fees: {
      ranges: [
        { min: 0, max: 50000, fee: 500 },
        { min: 50001, max: 200000, fee: 1000 },
        { min: 200001, max: 500000, fee: 2000 }
      ],
      percentage: 1,
      maximum: 5000
    },
    features: ['Instant processing', 'SMS notifications', 'Balance check', 'Transaction history'],
    supportedCurrencies: ['SLE'],
    limits: {
      min: 100,
      max: 2000000,
      daily: 5000000
    },
    popularity: 5,
    securityLevel: 'high',
    isRecommended: true
  },
  {
    id: PaymentMethod.AfricellMoney,
    name: 'Africell Money',
    displayName: 'Africell Money',
    icon: Smartphone,
    description: 'Fast and reliable mobile payments with Africell network',
    processingTime: 'Instant',
    availability: 'online',
    fees: {
      ranges: [
        { min: 0, max: 25000, fee: 250 },
        { min: 25001, max: 100000, fee: 750 },
        { min: 100001, max: 300000, fee: 1500 }
      ],
      percentage: 0.8,
      maximum: 4000
    },
    features: ['Real-time processing', 'Network coverage check', 'Low fees', 'Quick setup'],
    supportedCurrencies: ['SLE'],
    limits: {
      min: 100,
      max: 1500000,
      daily: 3000000
    },
    popularity: 4,
    securityLevel: 'high'
  },
  {
    id: PaymentMethod.BankTransfer,
    name: 'Bank Transfer',
    displayName: 'Bank Transfer',
    icon: Building2,
    description: 'Direct bank transfers from major Sierra Leone banks',
    processingTime: '1-3 business days',
    availability: 'limited',
    fees: {
      fixed: 2500,
      minimum: 2500,
      maximum: 15000
    },
    features: ['High security', 'Large amounts', 'Bank verification', 'Receipt provided'],
    supportedCurrencies: ['SLE', 'USD'],
    limits: {
      min: 10000,
      max: 50000000,
      daily: 100000000
    },
    popularity: 3,
    securityLevel: 'high'
  },
  {
    id: PaymentMethod.PayPal,
    name: 'PayPal',
    displayName: 'PayPal',
    icon: Globe,
    description: 'International payments for diaspora clients',
    processingTime: '1-2 business days',
    availability: 'online',
    fees: {
      percentage: 3.5,
      fixed: 2000,
      minimum: 2000
    },
    features: ['International support', 'Currency conversion', 'Buyer protection', 'Easy setup'],
    supportedCurrencies: ['USD', 'EUR', 'GBP', 'SLE'],
    limits: {
      min: 5000,
      max: 10000000,
      daily: 25000000
    },
    popularity: 4,
    securityLevel: 'high'
  },
  {
    id: PaymentMethod.Stripe,
    name: 'Stripe',
    displayName: 'Credit/Debit Card',
    icon: CreditCard,
    description: 'Secure card payments with international support',
    processingTime: 'Instant',
    availability: 'online',
    fees: {
      percentage: 2.9,
      fixed: 1500
    },
    features: ['Card support', 'Instant processing', 'Fraud protection', 'Global acceptance'],
    supportedCurrencies: ['USD', 'SLE'],
    limits: {
      min: 1000,
      max: 20000000,
      daily: 50000000
    },
    popularity: 3,
    securityLevel: 'high'
  }
]

interface PaymentMethodSelectorProps {
  selectedMethod?: PaymentMethod
  onMethodSelect: (method: PaymentMethod, provider: PaymentProvider) => void
  amount: number
  showFees?: boolean
  showFeatures?: boolean
  className?: string
}

export default function PaymentMethodSelector({
  selectedMethod,
  onMethodSelect,
  amount,
  showFees = true,
  showFeatures = true,
  className
}: PaymentMethodSelectorProps) {
  const [providersStatus, setProvidersStatus] = useState<Record<PaymentMethod, 'checking' | 'online' | 'offline' | 'limited'>>({
    [PaymentMethod.OrangeMoney]: 'checking',
    [PaymentMethod.AfricellMoney]: 'checking',
    [PaymentMethod.BankTransfer]: 'checking',
    [PaymentMethod.PayPal]: 'checking',
    [PaymentMethod.Stripe]: 'checking'
  })

  useEffect(() => {
    // Simulate checking provider availability
    const checkProviderStatus = async () => {
      // Stagger the status updates for realistic feel
      const delays = [500, 800, 1200, 1500, 2000]
      
      for (let i = 0; i < paymentProviders.length; i++) {
        setTimeout(() => {
          const provider = paymentProviders[i]
          // Simulate random availability (mostly online)
          const statusOptions: Array<'online' | 'offline' | 'limited'> = ['online', 'online', 'online', 'limited', 'offline']
          const randomStatus = statusOptions[Math.floor(Math.random() * statusOptions.length)]
          
          setProvidersStatus(prev => ({
            ...prev,
            [provider.id]: randomStatus
          }))
        }, delays[i])
      }
    }

    checkProviderStatus()
  }, [])

  const calculateFee = (provider: PaymentProvider, amount: number): number => {
    if (provider.fees.ranges) {
      // Range-based fees (Orange Money, Africell Money)
      for (const range of provider.fees.ranges) {
        if (amount >= range.min && amount <= range.max) {
          return range.fee
        }
      }
      // If amount exceeds all ranges, use percentage + max cap
      if (provider.fees.percentage) {
        const percentageFee = amount * (provider.fees.percentage / 100)
        return Math.min(percentageFee, provider.fees.maximum || percentageFee)
      }
    }

    // Fixed + percentage fees
    let fee = provider.fees.fixed || 0
    if (provider.fees.percentage) {
      fee += amount * (provider.fees.percentage / 100)
    }

    // Apply min/max limits
    if (provider.fees.minimum) {
      fee = Math.max(fee, provider.fees.minimum)
    }
    if (provider.fees.maximum) {
      fee = Math.min(fee, provider.fees.maximum)
    }

    return fee
  }

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'online':
        return <CheckCircle className="h-4 w-4 text-green-500" />
      case 'offline':
        return <AlertTriangle className="h-4 w-4 text-red-500" />
      case 'limited':
        return <Clock className="h-4 w-4 text-yellow-500" />
      case 'checking':
        return <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-500" />
      default:
        return null
    }
  }

  const getStatusText = (status: string) => {
    switch (status) {
      case 'online':
        return 'Available'
      case 'offline':
        return 'Unavailable'
      case 'limited':
        return 'Limited'
      case 'checking':
        return 'Checking...'
      default:
        return 'Unknown'
    }
  }

  const getSecurityIcon = (level: string) => {
    switch (level) {
      case 'high':
        return <Shield className="h-3 w-3 text-green-600" />
      case 'medium':
        return <Shield className="h-3 w-3 text-yellow-600" />
      case 'basic':
        return <Shield className="h-3 w-3 text-gray-600" />
      default:
        return null
    }
  }

  // Sort providers by recommendation, availability, and popularity
  const sortedProviders = [...paymentProviders].sort((a, b) => {
    const aStatus = providersStatus[a.id]
    const bStatus = providersStatus[b.id]
    
    // Recommended providers first
    if (a.isRecommended && !b.isRecommended) return -1
    if (!a.isRecommended && b.isRecommended) return 1
    
    // Available providers before unavailable
    if (aStatus === 'online' && bStatus !== 'online') return -1
    if (aStatus !== 'online' && bStatus === 'online') return 1
    
    // Sort by popularity
    return b.popularity - a.popularity
  })

  return (
    <div className={cn("space-y-4", className)}>
      <div className="flex items-center gap-2 mb-4">
        <CreditCard className="h-5 w-5 text-sierra-blue" />
        <h3 className="font-semibold text-sierra-blue">Select Payment Method</h3>
        <Badge variant="outline" className="text-xs">
          Amount: Le {amount.toLocaleString()}
        </Badge>
      </div>

      <div className="grid gap-3">
        {sortedProviders.map((provider) => {
          const status = providersStatus[provider.id]
          const fee = calculateFee(provider, amount)
          const total = amount + fee
          const isSelected = selectedMethod === provider.id
          const isAvailable = status === 'online' || status === 'limited'
          const IconComponent = provider.icon

          return (
            <Card
              key={provider.id}
              className={cn(
                "cursor-pointer transition-all duration-200 hover:shadow-md",
                isSelected && "ring-2 ring-sierra-blue border-sierra-blue",
                !isAvailable && "opacity-60",
                provider.isRecommended && "border-sierra-gold"
              )}
              onClick={() => isAvailable && onMethodSelect(provider.id, provider)}
            >
              <CardContent className="p-4">
                <div className="flex items-start justify-between">
                  <div className="flex items-start gap-3 flex-1">
                    <div className={cn(
                      "w-10 h-10 rounded-lg flex items-center justify-center",
                      provider.id === PaymentMethod.OrangeMoney && "bg-orange-100",
                      provider.id === PaymentMethod.AfricellMoney && "bg-red-100",
                      provider.id === PaymentMethod.BankTransfer && "bg-blue-100",
                      provider.id === PaymentMethod.PayPal && "bg-blue-100",
                      provider.id === PaymentMethod.Stripe && "bg-purple-100"
                    )}>
                      <IconComponent className={cn(
                        "h-5 w-5",
                        provider.id === PaymentMethod.OrangeMoney && "text-orange-600",
                        provider.id === PaymentMethod.AfricellMoney && "text-red-600",
                        provider.id === PaymentMethod.BankTransfer && "text-blue-600",
                        provider.id === PaymentMethod.PayPal && "text-blue-600",
                        provider.id === PaymentMethod.Stripe && "text-purple-600"
                      )} />
                    </div>
                    
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <h4 className="font-medium text-sm">{provider.displayName}</h4>
                        {provider.isRecommended && (
                          <Badge variant="secondary" className="text-xs bg-sierra-gold/10 text-sierra-gold">
                            <TrendingUp className="h-3 w-3 mr-1" />
                            Recommended
                          </Badge>
                        )}
                        <div className="flex items-center gap-1">
                          {getSecurityIcon(provider.securityLevel)}
                        </div>
                      </div>
                      
                      <p className="text-xs text-muted-foreground mb-2">
                        {provider.description}
                      </p>
                      
                      <div className="flex items-center gap-4 text-xs">
                        <div className="flex items-center gap-1">
                          {getStatusIcon(status)}
                          <span className={cn(
                            status === 'online' && "text-green-600",
                            status === 'offline' && "text-red-600",
                            status === 'limited' && "text-yellow-600",
                            status === 'checking' && "text-blue-600"
                          )}>
                            {getStatusText(status)}
                          </span>
                        </div>
                        
                        <div className="flex items-center gap-1">
                          <Zap className="h-3 w-3 text-muted-foreground" />
                          <span className="text-muted-foreground">{provider.processingTime}</span>
                        </div>
                        
                        <div className="flex">
                          {Array.from({ length: 5 }).map((_, i) => (
                            <div key={i} className={cn(
                              "w-2 h-2 rounded-full mr-0.5",
                              i < provider.popularity ? "bg-sierra-gold" : "bg-gray-200"
                            )} />
                          ))}
                        </div>
                      </div>

                      {showFeatures && provider.features && (
                        <div className="flex flex-wrap gap-1 mt-2">
                          {provider.features.slice(0, 3).map((feature, index) => (
                            <Badge key={index} variant="outline" className="text-xs">
                              {feature}
                            </Badge>
                          ))}
                        </div>
                      )}
                    </div>
                  </div>

                  {showFees && (
                    <div className="text-right ml-4">
                      <div className="text-sm font-medium">
                        Le {total.toLocaleString()}
                      </div>
                      <div className="text-xs text-muted-foreground">
                        Fee: Le {fee.toLocaleString()}
                      </div>
                      <div className="text-xs text-muted-foreground">
                        Limit: Le {provider.limits.max.toLocaleString()}
                      </div>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          )
        })}
      </div>

      {/* Information footer */}
      <div className="bg-blue-50 p-3 rounded-lg border border-blue-200">
        <div className="flex items-start gap-2">
          <Info className="h-4 w-4 text-blue-600 mt-0.5 flex-shrink-0" />
          <div className="text-xs text-blue-800">
            <p className="font-medium mb-1">Payment Information:</p>
            <p>
              Fees are calculated based on the payment amount. Processing times may vary depending on network conditions and provider availability.
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}