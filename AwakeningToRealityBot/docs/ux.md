# UX & UI Notes

## Layout

- **Desktop**: Two-panel layout with chat (left) and source viewer (right).  Chat panel hosts
  conversation history, input box, and model selector.  Source viewer renders PDFs with
  citation highlighting.
- **Mobile**: Single column layout with collapsible document selector and bottom-docked input
  area.

## Key components

1. **Document selector**
   - Multi-select dropdown with search.
   - Includes "All Documents" option pre-selected.
   - Chips display active filters; clicking removes selection.

2. **Chat timeline**
   - User messages right-aligned; AI messages left-aligned.
   - Thinking models display animated placeholder until Durable Function completes.
   - Each AI message appends citation tokens `[1]`, `[2]` that map to retrieved chunks.

3. **Source viewer**
   - Uses PDF.js via JavaScript interop to render the PDF in the right panel.
   - Citation clicks trigger smooth scroll and highlight overlay on the relevant text.

4. **Model selector**
   - Dropdown listing available models for the user's tier.
   - Icons: ⚡ for fast models, 🧠 for thinking models.
   - Tooltip summarises latency/cost trade-offs.

5. **Admin dashboard**
   - Tabbed interface: Overview, Tier Management, Model Assignment, User Management,
     Content Management.
   - Tier tab contains toggle switches, pricing inputs, quota controls, and usage charts.
   - Content tab exposes PDF upload list with status (ingested / processing / failed).

## Accessibility

- Support keyboard navigation throughout the SPA.
- Provide ARIA labels for dropdowns and toggles.
- Maintain minimum contrast ratio of 4.5:1.
- Allow users to expand citations as plain text for screen readers.
