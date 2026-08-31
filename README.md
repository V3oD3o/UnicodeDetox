# UnicodeDetox

**Streaming filter for AI‑generated Markdown that removes typographic Unicode glitter (smart quotes, ellipsis, dashes, bullets, arrows, etc.) while preserving all code blocks and inline code.**  
The output remains in the same encoding as the input.

---

## Why this exists

AI‑generated Markdown often contains typographic Unicode characters that make the text look artificial:

- “smart” quotes  
- … ellipsis  
- — em dash / – en dash  
- → ← ↔ arrows  
- • and · bullets  
- ★ ☆ decorative stars  

These characters are visually loud and immediately reveal machine‑generated content.  
**UnicodeDetox removes them while keeping Markdown structure intact.**

---

## What it does

### ✔ Streaming processing
Processes input incrementally from STDIN without loading the entire file.

### ✔ BOM detection
Detects UTF‑8 and UTF‑16 BOMs and selects the correct encoding.  
The output uses the same encoding.

### ✔ Code‑safe
- Inline code (backtick runs) is preserved  
- Fenced code blocks are preserved  
- Escaped backticks are handled correctly  
- No detoxing happens inside code

### ✔ Typographic Unicode cleanup
A curated set of typographic Unicode characters is replaced with simpler equivalents:

- smart quotes → `"`, `'`  
- ellipsis → `...`  
- en dash → `-`  
- arrows → `->`, `<-`, `<->`  
- bullets → `*`  
- check marks → `[OK]`, `[X]`  
- multiplication/division signs → `x`, `/`  
- guillemets → `<<`, `>>`

### ✔ EM DASH formatting cleanup

The em dash (`—`) is normalized based on surrounding context:

- `a—b` → `a - b`  
- `a — b` → `a - b`

This removes the tight, AI‑style em‑dash formatting and produces consistent spacing.

> Unicode spaces are **not** replaced; they are only used to detect context.

---

## What it does *not* do

- Does **not** parse Markdown  
- Does **not** syntax‑highlight  
- Does **not** detect AI text  
- Does **not** convert the entire document to ASCII  
- Does **not** modify code blocks or inline code  
- Does **not** change the encoding of the output  
- Does **not** normalize Unicode spaces

---

## Usage

### CLI

```bash
cat input.md | UnicodeDetox > output.md
```

or

```bash
UnicodeDetox < ai_generated.md > cleaned.md
```

---

## Example (inline code + code block + EM DASH examples)

Input:

````markdown
Here is some AI‑style text: “smart quotes”… and an em dash example: a—b becomes a - b, and a — b also becomes a - b.  
Inline code stays untouched: `a—b`.

And here is a code block:

```csharp
Console.WriteLine("a—b should remain untouched inside code blocks");
```
````

Output:

````markdown
Here is some AI‑style text: "smart quotes"... and an em dash example: a - b becomes a - b, and a - b also becomes a - b.  
Inline code stays untouched: `a—b`.

And here is a code block:

```csharp
Console.WriteLine("a—b should remain untouched inside code blocks");
```
````

---

## License

Apache License 2.0
