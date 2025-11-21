# Page snapshot

```yaml
- generic [active] [ref=e1]:
  - link "Skip to main content" [ref=e2]:
    - /url: "#main-content"
  - link "Skip to navigation" [ref=e3]:
    - /url: "#navigation"
  - generic [ref=e5]:
    - generic [ref=e6]:
      - img [ref=e8]
      - generic [ref=e10]: Something went wrong
      - generic [ref=e11]: An unexpected error occurred. Please try refreshing the page or contact support if the problem persists.
    - generic [ref=e13]:
      - generic [ref=e14]: Unexpected token ')'
      - button "Try Again" [ref=e15] [cursor=pointer]:
        - img
        - text: Try Again
  - generic [ref=e20] [cursor=pointer]:
    - button "Open Next.js Dev Tools" [ref=e21] [cursor=pointer]:
      - img [ref=e22] [cursor=pointer]
    - generic [ref=e27] [cursor=pointer]:
      - button "Open issues overlay" [ref=e28] [cursor=pointer]:
        - generic [ref=e29] [cursor=pointer]:
          - generic [ref=e30] [cursor=pointer]: "1"
          - generic [ref=e31] [cursor=pointer]: "2"
        - generic [ref=e32] [cursor=pointer]:
          - text: Issue
          - generic [ref=e33] [cursor=pointer]: s
      - button "Collapse issues badge" [ref=e34] [cursor=pointer]:
        - img [ref=e35] [cursor=pointer]
  - alert [ref=e37]
```