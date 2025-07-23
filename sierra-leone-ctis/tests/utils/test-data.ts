// Test user credentials and data
export const TEST_USERS = {
  admin: {
    email: 'admin@bettsfirm.sl',
    password: 'Admin123!',
    role: 'Admin',
    name: 'System Admin'
  },
  associate: {
    email: 'associate@bettsfirm.sl',
    password: 'Associate123!',
    role: 'Associate',
    name: 'Tax Associate'
  },
  client: {
    email: 'client@testcompany.sl',
    password: 'Client123!',
    role: 'Client',
    name: 'Test Company',
    businessName: 'Test Company Ltd'
  },
  invalidUser: {
    email: 'invalid@example.com',
    password: 'wrongpassword'
  }
};

export const TEST_CLIENT_DATA = {
  businessName: 'E2E Test Company Ltd',
  contactPerson: 'John Doe',
  email: 'e2etest@company.sl',
  phoneNumber: '+232 76 123 456',
  address: '123 Test Street, Freetown, Sierra Leone',
  tin: '1234567890'
};

export const TEST_DOCUMENT = {
  fileName: 'test-document.pdf',
  type: 'tax_return',
  description: 'Test tax return document for E2E testing',
  taxYear: new Date().getFullYear() - 1
};

export const API_ENDPOINTS = {
  auth: {
    login: '/api/auth/login',
    logout: '/api/auth/logout',
    register: '/api/auth/register'
  },
  clientPortal: {
    dashboard: '/api/client-portal/dashboard',
    documents: '/api/client-portal/documents',
    profile: '/api/client-portal/profile',
    payments: '/api/client-portal/payments',
    taxFilings: '/api/client-portal/tax-filings'
  },
  admin: {
    clients: '/api/admin/clients',
    stats: '/api/admin/clients/stats'
  }
};

export const ROUTES = {
  home: '/',
  login: '/login',
  dashboard: '/dashboard',
  clientPortal: {
    dashboard: '/client-portal/dashboard',
    documents: '/client-portal/documents',
    profile: '/client-portal/profile',
    payments: '/client-portal/payments',
    taxFilings: '/client-portal/tax-filings',
    compliance: '/client-portal/compliance',
    deadlines: '/client-portal/deadlines'
  },
  admin: {
    associates: '/admin/associates',
    settings: '/admin/settings'
  },
  associate: {
    dashboard: '/associate/dashboard',
    permissions: '/associate/permissions',
    clients: '/associate/clients'
  }
};

export const SELECTORS = {
  // Common selectors
  loading: '[data-testid="loading"]',
  error: '[data-testid="error"]',
  
  // Navigation
  sidebar: '[data-testid="sidebar"]',
  clientSidebar: '[data-testid="client-sidebar"]',
  
  // Forms
  form: 'form',
  submitButton: 'button[type="submit"]',
  
  // Login form
  loginForm: '[data-testid="login-form"]',
  emailInput: 'input[name="email"]',
  passwordInput: 'input[name="password"]',
  loginButton: 'button[type="submit"]',
  
  // Client portal specific
  dashboardTitle: '[data-testid="dashboard-title"]',
  complianceScore: '[data-testid="compliance-score"]',
  quickActions: '[data-testid="quick-actions"]',
  recentActivity: '[data-testid="recent-activity"]',
  
  // Document management
  uploadButton: '[data-testid="upload-document"]',
  fileInput: 'input[type="file"]',
  documentList: '[data-testid="document-list"]',
  downloadButton: '[data-testid="download-document"]',
  
  // Profile form
  profileForm: '[data-testid="profile-form"]',
  businessNameInput: 'input[name="businessName"]',
  saveButton: '[data-testid="save-profile"]',
  
  // Associate system selectors
  associateDashboard: '[data-testid="associate-dashboard"]',
  associateNavigation: '[data-testid="associate-nav"]',
  adminNavigation: '[data-testid="admin-nav"]',
  grantPermissionButton: 'button:has-text("Grant Permission")',
  grantPermissionDialog: '[data-testid="grant-permission-dialog"]',
  permissionFilters: '[data-testid="permission-filters"]',
  clientCards: '[data-testid="client-card"]',
  permissionRows: '[data-testid="permission-row"]',
  summaryCards: '[data-testid="summary-card"]'
};

export const TIMEOUTS = {
  short: 5000,
  medium: 10000,
  long: 30000,
  upload: 60000
};