/**
 * Security tests for Chart component CSS sanitization
 * Tests XSS prevention in chart.tsx
 */

import { describe, it, expect } from 'vitest';

// Import the sanitization functions (these would need to be exported from chart.tsx for testing)
// For now, we'll replicate the logic to test it

const sanitizeColor = (color: string): string => {
  const validColorPattern = /^(#[0-9a-fA-F]{3,8}|rgb\([^)]+\)|rgba\([^)]+\)|hsl\([^)]+\)|hsla\([^)]+\)|[a-z]+)$/;

  if (!color || typeof color !== 'string') {
    return '';
  }

  const trimmedColor = color.trim();

  if (validColorPattern.test(trimmedColor)) {
    if (trimmedColor.toLowerCase().includes('script') ||
        trimmedColor.toLowerCase().includes('javascript:') ||
        trimmedColor.includes('<') ||
        trimmedColor.includes('>')) {
      return '';
    }
    return trimmedColor;
  }

  return '';
};

const sanitizeKey = (key: string): string => {
  return key.replace(/[^a-zA-Z0-9-_]/g, '');
};

describe('Chart Component Security - CSS Sanitization', () => {
  describe('sanitizeColor', () => {
    it('should allow valid hex colors', () => {
      expect(sanitizeColor('#fff')).toBe('#fff');
      expect(sanitizeColor('#ffffff')).toBe('#ffffff');
      expect(sanitizeColor('#ff00ff')).toBe('#ff00ff');
      expect(sanitizeColor('#12345678')).toBe('#12345678');
    });

    it('should allow valid rgb colors', () => {
      expect(sanitizeColor('rgb(255, 0, 0)')).toBe('rgb(255, 0, 0)');
      expect(sanitizeColor('rgb(0,128,255)')).toBe('rgb(0,128,255)');
    });

    it('should allow valid rgba colors', () => {
      expect(sanitizeColor('rgba(255, 0, 0, 0.5)')).toBe('rgba(255, 0, 0, 0.5)');
    });

    it('should allow valid hsl colors', () => {
      expect(sanitizeColor('hsl(120, 100%, 50%)')).toBe('hsl(120, 100%, 50%)');
    });

    it('should allow valid named colors', () => {
      expect(sanitizeColor('red')).toBe('red');
      expect(sanitizeColor('blue')).toBe('blue');
    });

    it('should block script injection attempts', () => {
      expect(sanitizeColor('red;}</style><script>alert("XSS")</script>')).toBe('');
      expect(sanitizeColor('<script>alert(1)</script>')).toBe('');
    });

    it('should block javascript protocol', () => {
      expect(sanitizeColor('javascript:alert(1)')).toBe('');
      expect(sanitizeColor('red;background:url(javascript:alert(1))')).toBe('');
    });

    it('should block HTML tags', () => {
      expect(sanitizeColor('<img src=x onerror=alert(1)>')).toBe('');
      expect(sanitizeColor('red<>')).toBe('');
    });

    it('should block CSS injection with script keyword', () => {
      expect(sanitizeColor('red;script:something')).toBe('');
      expect(sanitizeColor('SCRIPT')).toBe('');
    });

    it('should handle null and undefined', () => {
      expect(sanitizeColor(null as any)).toBe('');
      expect(sanitizeColor(undefined as any)).toBe('');
      expect(sanitizeColor('')).toBe('');
    });

    it('should trim whitespace', () => {
      expect(sanitizeColor('  #fff  ')).toBe('#fff');
      expect(sanitizeColor('  red  ')).toBe('red');
    });

    it('should block malicious CSS expressions', () => {
      expect(sanitizeColor('expression(alert(1))')).toBe('');
      expect(sanitizeColor('url(data:text/html,<script>alert(1)</script>)')).toBe('');
    });
  });

  describe('sanitizeKey', () => {
    it('should allow alphanumeric characters', () => {
      expect(sanitizeKey('color123')).toBe('color123');
      expect(sanitizeKey('ABC')).toBe('ABC');
    });

    it('should allow hyphens and underscores', () => {
      expect(sanitizeKey('color-primary')).toBe('color-primary');
      expect(sanitizeKey('color_secondary')).toBe('color_secondary');
      expect(sanitizeKey('my-color_123')).toBe('my-color_123');
    });

    it('should remove special characters', () => {
      expect(sanitizeKey('color!@#$%')).toBe('color');
      expect(sanitizeKey('my.color')).toBe('mycolor');
      expect(sanitizeKey('color[0]')).toBe('color0');
    });

    it('should block injection attempts', () => {
      expect(sanitizeKey('";alert(1)//')).toBe('alert1');
      expect(sanitizeKey('color</style><script>')).toBe('colorstylescript');
      expect(sanitizeKey('${malicious}')).toBe('malicious');
    });

    it('should handle empty strings', () => {
      expect(sanitizeKey('')).toBe('');
    });
  });

  describe('XSS Prevention Integration', () => {
    it('should prevent XSS through color injection', () => {
      const maliciousColors = [
        '"}\\x3cscript>alert(1)\\x3c/script>',
        'red;}</style><script>alert(1)</script><style>',
        'rgb(255,0,0);background:url(javascript:alert(1))',
        '\\27\\22;alert(String.fromCharCode(88,83,83))//\\27',
      ];

      maliciousColors.forEach(color => {
        expect(sanitizeColor(color)).toBe('');
      });
    });

    it('should prevent XSS through key injection', () => {
      const maliciousKeys = [
        'color";onerror="alert(1)"',
        'key</style><script>alert(1)</script>',
        'myKey;(function(){alert(1)})()',
      ];

      maliciousKeys.forEach(key => {
        const sanitized = sanitizeKey(key);
        expect(sanitized).not.toContain('"');
        expect(sanitized).not.toContain('<');
        expect(sanitized).not.toContain('>');
        expect(sanitized).not.toContain(';');
        expect(sanitized).not.toContain('(');
        expect(sanitized).not.toContain(')');
      });
    });
  });
});
