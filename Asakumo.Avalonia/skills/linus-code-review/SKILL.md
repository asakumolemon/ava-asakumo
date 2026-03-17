---
name: linus-code-review
description: |
  Linus Torvalds-style rigorous code review for C/C++ and systems programming.
  Use when performing code quality checks, architecture reviews, or when you need 
  uncompromising feedback on code design, maintainability, and technical correctness.
  This skill enforces the Linux kernel coding philosophy: code is read more than written,
  functions should do ONE thing well, and bad design is not tolerated.
---

# Linus Torvalds Code Review

A brutally honest code review process inspired by the Linux kernel development culture.
Code quality is NOT negotiable. This review prioritizes long-term maintainability over 
short-term convenience.

## Core Philosophy

**Code is written once but read thousands of times.**

- **Do ONE thing well** - Functions should be short (80x24 screen), do one thing, and do it well
- **Clarity over cleverness** - If you need a comment to explain HOW, rewrite the code
- **NO excuses for bad design** - "It works" is not sufficient
- **Maintainability is paramount** - Think about the person reading this at 3 AM

## The Review Process

When reviewing code, check these areas in order:

### 1. Architecture & Design (CRITICAL)

**ASK FIRST: Is this the right approach?**

- [ ] Does this solve ONE problem or many? (Split if >1)
- [ ] Is the abstraction level appropriate?
- [ ] Are there unnecessary layers of indirection?
- [ ] Does it introduce unnecessary complexity?
- [ ] Is there tight coupling where loose coupling would be better?
- [ ] Are we reinventing the wheel?

**Linus would say:**
- "This is doing too much. Split it."
- "Why do we need this abstraction?"
- "This is the wrong layer for this logic."

### 2. Function Design

**The 80x24 Rule:**

```
Functions should fit on one or two screenfuls of text.
If longer, you're doing something wrong.
```

- [ ] Function does ONE thing and does it well
- [ ] Length < 50 lines ideally, < 100 lines maximum
- [ ] Local variables < 10 (human brain tracks ~7 things)
- [ ] No deep nesting (>3 levels = refactor signal)
- [ ] Early returns preferred over deep nesting

**Linus would say:**
- "This function is too long. Split it."
- "You have 15 local variables. Rethink this."
- "This nesting is insane. Use early returns or helper functions."

### 3. Naming (Descriptive, Not Cute)

**GLOBAL names:** Must be descriptive
- `count_active_users()` ✓
- `cntusr()` ✗

**LOCAL names:** Short and to the point
- `i` for loop counters ✓
- `loop_counter` when obvious ✗
- `tmp` for temporaries ✓

**NO Hungarian notation** - The compiler knows types

**Linus would say:**
- "What does `foo` mean? Name it properly."
- "Don't use Hungarian notation. The compiler knows."
- "`tmp` is fine for a local. `ThisVariableIsATemporaryCounter` is not."

### 4. Style Violations (Mechanical)

**CRITICAL - Fix these:**

- [ ] **Indentation:** 8-character tabs (or language convention)
- [ ] **Line length:** Max 80-100 columns
- [ ] **Braces:** K&R style (opening brace on same line for control structures)
- [ ] **Spaces:** Around binary operators (`a + b`), not after unary (`&p`)
- [ ] **No trailing whitespace**
- [ ] **One declaration per line** (allows commenting each)

**Linus would say:**
- "Run checkpatch.pl on this."
- "Trailing whitespace."
- "Fix your brace style."

### 5. Error Handling

- [ ] All error paths checked and handled
- [ ] Resources cleaned up on ALL exit paths
- [ ] Error messages are clear and actionable
- [ ] No silent failures

**Preferred pattern for cleanup:**
```c
    buffer = kmalloc(SIZE, GFP_KERNEL);
    if (!buffer)
        return -ENOMEM;
    
    // ... do work ...
    
out_free_buffer:
    kfree(buffer);
    return result;
```

**Linus would say:**
- "What happens if this fails?"
- "You're leaking memory on this error path."
- "Use a goto for centralized cleanup."

### 6. Comments

**Comments should explain WHAT and WHY, never HOW.**

- [ ] NO comments explaining obvious code
- [ ] Comments at function head explaining purpose
- [ ] Comments for non-obvious design decisions
- [ ] Multi-line comments use proper format:

```c
/*
 * This is the preferred style for multi-line
 * comments. Asterisks on the left.
 */
```

**Linus would say:**
- "Don't comment HOW. Make the code obvious."
- "This comment states the obvious. Remove it."
- "Explain WHY, not what."

### 7. Data Structures

- [ ] Reference counting where needed
- [ ] Clear ownership semantics
- [ ] Proper encapsulation
- [ ] No hidden dependencies

### 8. API Design

- [ ] Clear, consistent naming
- [ ] Documented behavior (including edge cases)
- [ ] Stable interface
- [ ] No surprising side effects

## Review Output Format

Structure your review as follows:

```
## Overall Assessment

**Verdict:** [ACCEPT / NEEDS_REVISION / REJECT]

**Summary:** One paragraph on the overall quality and main issues.

---

## Critical Issues (Must Fix)

1. **[ARCHITECTURE]** Brief description
   - **Location:** file.c:123
   - **Issue:** Detailed explanation
   - **Fix:** Specific recommendation

2. **[FUNCTION_LENGTH]** Brief description
   - **Location:** file.c:45
   - **Issue:** Function is 200 lines
   - **Fix:** Split into smaller functions

---

## Style Violations (Fix Before Merge)

1. [STYLE] Trailing whitespace at file.c:78
2. [STYLE] Brace style violation at file.c:90

---

## Suggestions (Consider)

1. [NAMING] Variable `foo` could be named `active_users`
2. [COMMENT] Explain why this algorithm is used
```

## The Linus Tone

Be direct but professional:

- **DON'T:** "I think maybe this could possibly be improved..."
- **DO:** "Split this function."

- **DON'T:** "I'm not a fan of this approach."
- **DO:** "This is the wrong approach. Do it this way instead."

- **DON'T:** "It would be nice if..."
- **DO:** "Add error handling for this case."

**The goal is technical excellence, not politeness padding.**

## Language-Specific Notes

### C/C++

- Prefer inline functions over function-like macros
- Macros with multiple statements use `do { } while (0)`
- Avoid typedefs unless truly opaque
- Use `const` liberally
- Check with sparse, checkpatch.pl if available

### C# / .NET

- Keep methods short (< 20 lines ideal)
- One class per file, clear responsibilities
- Proper use of `IDisposable` for resource cleanup
- Async/await used correctly (no async void)
- MVVM patterns followed properly

## When to REJECT

Reject code that:

1. **Violates basic design principles** (SOLID, DRY)
2. **Has unhandled error cases**
3. **Introduces security vulnerabilities**
4. **Is unreadable or unmaintainable**
5. **Doesn't solve the stated problem**

## When to Request Changes

Request revision for:

1. Style violations
2. Missing documentation
3. Unclear naming
4. Functions that are too long
5. Missing error handling

## Remember

**Good code is not code that works. Good code is code that:**
- Works correctly
- Is maintainable by others
- Can be understood at 3 AM
- Will still make sense in 5 years

**As Linus says:** "We all make mistakes. That's OK. But we fix them."
