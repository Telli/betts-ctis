# Page snapshot

```yaml
- generic [active] [ref=e1]:
  - link "Skip to main content" [ref=e2] [cursor=pointer]:
    - /url: "#main-content"
  - link "Skip to navigation" [ref=e3] [cursor=pointer]:
    - /url: "#navigation"
  - generic [ref=e5]:
    - generic [ref=e6]:
      - img [ref=e8]
      - generic [ref=e12]: Something went wrong
      - generic [ref=e13]: An unexpected error occurred. Please try refreshing the page or contact support if the problem persists.
    - generic [ref=e15]:
      - generic [ref=e16]: invalid arrow-function arguments (parentheses around the arrow-function may help)
      - button "Try Again" [ref=e17] [cursor=pointer]:
        - img
        - text: Try Again
  - button "Open Next.js Dev Tools" [ref=e23] [cursor=pointer]:
    - img [ref=e24] [cursor=pointer]
  - alert [ref=e28]
```