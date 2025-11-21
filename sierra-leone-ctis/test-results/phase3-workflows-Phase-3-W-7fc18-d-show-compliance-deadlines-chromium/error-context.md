# Page snapshot

```yaml
- generic [active] [ref=e1]:
  - link "Skip to main content" [ref=e2] [cursor=pointer]:
    - /url: "#main-content"
  - link "Skip to navigation" [ref=e3] [cursor=pointer]:
    - /url: "#navigation"
  - main "Main content" [ref=e5]:
    - generic [ref=e7]:
      - img [ref=e8]
      - paragraph [ref=e10]: Loading workflow dashboard…
  - region "Notifications (F8)":
    - list
```