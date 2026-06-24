const fs = require("fs");
const path = require("path");
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  AlignmentType, HeadingLevel, BorderStyle, WidthType, ShadingType,
  VerticalAlign, PageBreak, LevelFormat,
} = require("docx");

const CONTENT_W = 9360; // US Letter, 1" margins
const border = { style: BorderStyle.SINGLE, size: 1, color: "B0B0B0" };
const borders = { top: border, bottom: border, left: border, right: border };
const cellMargins = { top: 80, bottom: 80, left: 120, right: 120 };
const HEADER_FILL = "1F3A5F"; // QuantWise dark ink
const ZEBRA = "F2F5F8";

function h(text, opts = {}) {
  return new Paragraph({
    children: [new TextRun({ text, bold: true, color: opts.color || "FFFFFF", size: opts.size || 20 })],
    alignment: opts.align || AlignmentType.LEFT,
    spacing: { before: 0, after: 0 },
  });
}
function p(text, opts = {}) {
  return new Paragraph({
    children: [new TextRun({ text, bold: !!opts.bold, italics: !!opts.italics, size: opts.size || 22, color: opts.color })],
    alignment: opts.align || AlignmentType.LEFT,
    spacing: { before: opts.before ?? 0, after: opts.after ?? 60 },
  });
}

// SUS statements (Brooke, 1996 standard wording)
const items = [
  "I think that I would like to use this system frequently.",
  "I found the system unnecessarily complex.",
  "I thought the system was easy to use.",
  "I think that I would need the support of a technical person to be able to use this system.",
  "I found the various functions in this system were well integrated.",
  "I thought there was too much inconsistency in this system.",
  "I would imagine that most people would learn to use this system very quickly.",
  "I found the system very cumbersome to use.",
  "I felt very confident using the system.",
  "I needed to learn a lot of things before I could get going with this system.",
];

// ---- SUS table ----
const numW = 520, stmtW = 4840, rateW = 800; // 520+4840+5*800 = 9360
const ratingHeaders = [
  "1\nStrongly\ndisagree", "2", "3", "4", "5\nStrongly\nagree",
];

function headerCell(width, lines) {
  const paras = lines.split("\n").map((l) =>
    new Paragraph({ children: [new TextRun({ text: l, bold: true, color: "FFFFFF", size: 16 })], alignment: AlignmentType.CENTER, spacing: { after: 0 } }));
  return new TableCell({ borders, width: { size: width, type: WidthType.DXA }, shading: { fill: HEADER_FILL, type: ShadingType.CLEAR }, margins: cellMargins, verticalAlign: VerticalAlign.CENTER, children: paras });
}

const headerRow = new TableRow({
  tableHeader: true,
  children: [
    new TableCell({ borders, width: { size: numW, type: WidthType.DXA }, shading: { fill: HEADER_FILL, type: ShadingType.CLEAR }, margins: cellMargins, verticalAlign: VerticalAlign.CENTER, children: [new Paragraph({ children: [new TextRun({ text: "#", bold: true, color: "FFFFFF", size: 18 })], alignment: AlignmentType.CENTER })] }),
    new TableCell({ borders, width: { size: stmtW, type: WidthType.DXA }, shading: { fill: HEADER_FILL, type: ShadingType.CLEAR }, margins: cellMargins, verticalAlign: VerticalAlign.CENTER, children: [new Paragraph({ children: [new TextRun({ text: "Statement", bold: true, color: "FFFFFF", size: 18 })] })] }),
    ...ratingHeaders.map((t) => headerCell(rateW, t)),
  ],
});

const itemRows = items.map((text, i) => {
  const shade = i % 2 === 1 ? { fill: ZEBRA, type: ShadingType.CLEAR } : undefined;
  return new TableRow({
    children: [
      new TableCell({ borders, width: { size: numW, type: WidthType.DXA }, shading: shade, margins: cellMargins, verticalAlign: VerticalAlign.CENTER, children: [new Paragraph({ children: [new TextRun({ text: String(i + 1), size: 20 })], alignment: AlignmentType.CENTER })] }),
      new TableCell({ borders, width: { size: stmtW, type: WidthType.DXA }, shading: shade, margins: cellMargins, verticalAlign: VerticalAlign.CENTER, children: [new Paragraph({ children: [new TextRun({ text, size: 20 })] })] }),
      ...[0, 1, 2, 3, 4].map(() => new TableCell({ borders, width: { size: rateW, type: WidthType.DXA }, shading: shade, margins: cellMargins, verticalAlign: VerticalAlign.CENTER, children: [new Paragraph({ children: [new TextRun({ text: "", size: 20 })], alignment: AlignmentType.CENTER })] })),
    ],
  });
});

const susTable = new Table({ width: { size: CONTENT_W, type: WidthType.DXA }, columnWidths: [numW, stmtW, rateW, rateW, rateW, rateW, rateW], rows: [headerRow, ...itemRows] });

// ---- Participant details table ----
function detailRow(label, value) {
  return new TableRow({ children: [
    new TableCell({ borders, width: { size: 3000, type: WidthType.DXA }, shading: { fill: ZEBRA, type: ShadingType.CLEAR }, margins: cellMargins, children: [new Paragraph({ children: [new TextRun({ text: label, bold: true, size: 20 })] })] }),
    new TableCell({ borders, width: { size: 6360, type: WidthType.DXA }, margins: cellMargins, children: [new Paragraph({ children: [new TextRun({ text: value, size: 20 })] })] }),
  ] });
}
const detailsTable = new Table({ width: { size: CONTENT_W, type: WidthType.DXA }, columnWidths: [3000, 6360], rows: [
  detailRow("Participant ID", "P _______"),
  detailRow("Date", "____ / ____ / 2026"),
  detailRow("Facilitator", "_______________________"),
  detailRow("Device / browser", "_______________________"),
] });

// ---- Tasks performed list ----
const tasks = [
  "Sign up for an account and reach the onboarding screen.",
  "Complete the four-step risk questionnaire and view your risk profile.",
  "Open the dashboard and read one recommendation (the BUY/SELL/HOLD pick, its reason, and the suggested amount).",
  "Open the notifications bell and read a notification.",
  "Go to the Market view and search for a stock by name or symbol.",
];

// ---- Open questions ----
function answerLines(n) {
  const out = [];
  for (let i = 0; i < n; i++) out.push(new Paragraph({ border: { bottom: { style: BorderStyle.SINGLE, size: 4, color: "C8C8C8", space: 6 } }, spacing: { before: 120, after: 0 }, children: [new TextRun({ text: "" })] }));
  return out;
}

const numbering = { config: [
  { reference: "tasks", levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 540, hanging: 320 } } } }] },
] };

const doc = new Document({
  numbering,
  styles: {
    default: { document: { run: { font: "Arial", size: 22 } } },
    paragraphStyles: [
      { id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true, run: { size: 30, bold: true, color: "1F3A5F", font: "Arial" }, paragraph: { spacing: { before: 240, after: 120 }, outlineLevel: 0 } },
      { id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true, run: { size: 24, bold: true, color: "1F3A5F", font: "Arial" }, paragraph: { spacing: { before: 200, after: 100 }, outlineLevel: 1 } },
    ],
  },
  sections: [{
    properties: { page: { size: { width: 12240, height: 15840 }, margin: { top: 1080, right: 1440, bottom: 1080, left: 1440 } } },
    children: [
      new Paragraph({ children: [new TextRun({ text: "QuantWise", bold: true, size: 36, color: "1F3A5F" }), new TextRun({ text: "  —  Usability Study", size: 28, color: "555555" })], spacing: { after: 40 } }),
      new Paragraph({ children: [new TextRun({ text: "System Usability Scale (SUS) questionnaire", italics: true, size: 22, color: "555555" })], spacing: { after: 160 }, border: { bottom: { style: BorderStyle.SINGLE, size: 6, color: "1F3A5F", space: 6 } } }),

      detailsTable,

      new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun("Before you start")] }),
      p("Thank you for helping us test QuantWise, a tool that gives first-time investors a daily, plain-language stock recommendation. There are no right or wrong answers here, and we are testing the software, not you. Work through the tasks below at your own pace. If something is unclear, say so out loud; that feedback is exactly what we are after.", { after: 100 }),

      new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun("Tasks to complete")] }),
      ...tasks.map((t) => new Paragraph({ numbering: { reference: "tasks", level: 0 }, spacing: { after: 40 }, children: [new TextRun({ text: t, size: 22 })] })),

      new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun("Rate each statement")] }),
      p("Once you have finished the tasks, mark one box per row to show how much you agree with each statement (1 = strongly disagree, 5 = strongly agree). Please answer every row, and go with your first instinct.", { after: 120 }),

      susTable,

      new Paragraph({ children: [new PageBreak()] }),

      new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun("A few open questions")] }),
      p("What helped you most while using QuantWise?", { bold: true, after: 0 }),
      ...answerLines(3),
      p("What got in the way, confused you, or felt harder than it should have been?", { bold: true, before: 160, after: 0 }),
      ...answerLines(3),
      p("Was there anything you expected to find but couldn't?", { bold: true, before: 160, after: 0 }),
      ...answerLines(3),

      // Facilitator scoring helper
      new Paragraph({ children: [new PageBreak()] }),
      new Paragraph({ heading: HeadingLevel.HEADING_1, children: [new TextRun("Facilitator use only — scoring")] }),
      p("The SUS produces one score from 0 to 100. It is not a percentage. Convert each answer to a 0–4 point value, sum the ten, then multiply by 2.5.", { after: 80 }),
      p("Odd-numbered items (1, 3, 5, 7, 9): point value = response − 1.", { after: 20 }),
      p("Even-numbered items (2, 4, 6, 8, 10): point value = 5 − response.", { after: 80 }),
      p("Worked example: a response of 4 on item 1 scores 3; a response of 2 on item 2 scores 3. Add all ten point values (range 0–40), then × 2.5 → final SUS (0–100). As a rough guide, the long-run average across studies sits near 68; above that is better than average.", { after: 120 }),

      // scoring grid
      (function () {
        const colW = [780, 2080, 1500, 780, 2080, 1500]; // 8720... adjust to 9360
        // recompute: two halves: item | response | points  x2
        const w = [800, 1500, 1380, 800, 1500, 1380]; // sum 7360 -> pad
        const half = (n) => [
          new TableCell({ borders, width: { size: 800, type: WidthType.DXA }, shading: { fill: HEADER_FILL, type: ShadingType.CLEAR }, margins: cellMargins, children: [new Paragraph({ alignment: AlignmentType.CENTER, children: [new TextRun({ text: "Item", bold: true, color: "FFFFFF", size: 18 })] })] }),
          new TableCell({ borders, width: { size: 1880, type: WidthType.DXA }, shading: { fill: HEADER_FILL, type: ShadingType.CLEAR }, margins: cellMargins, children: [new Paragraph({ alignment: AlignmentType.CENTER, children: [new TextRun({ text: "Response (1–5)", bold: true, color: "FFFFFF", size: 18 })] })] }),
          new TableCell({ borders, width: { size: 1880, type: WidthType.DXA }, shading: { fill: HEADER_FILL, type: ShadingType.CLEAR }, margins: cellMargins, children: [new Paragraph({ alignment: AlignmentType.CENTER, children: [new TextRun({ text: "Points (0–4)", bold: true, color: "FFFFFF", size: 18 })] })] }),
        ];
        const cols = [800, 1880, 1880, 800, 1880, 1880]; // 9120
        const headerR = new TableRow({ tableHeader: true, children: [...half(), ...half()] });
        const rows = [];
        for (let r = 0; r < 5; r++) {
          const left = r + 1, right = r + 6;
          const cellNum = (n) => new TableCell({ borders, width: { size: 800, type: WidthType.DXA }, margins: cellMargins, children: [new Paragraph({ alignment: AlignmentType.CENTER, children: [new TextRun({ text: String(n), size: 20, bold: true })] })] });
          const blank = (wd) => new TableCell({ borders, width: { size: wd, type: WidthType.DXA }, margins: cellMargins, children: [new Paragraph({ children: [new TextRun("")] })] });
          rows.push(new TableRow({ children: [cellNum(left), blank(1880), blank(1880), cellNum(right), blank(1880), blank(1880)] }));
        }
        return new Table({ width: { size: 9120, type: WidthType.DXA }, columnWidths: cols, rows: [headerR, ...rows] });
      })(),

      p("Sum of points (0–40): _________      ×  2.5  =  SUS score (0–100): _________", { bold: true, before: 160 }),
    ],
  }],
});

const outPath = path.join(__dirname, "QuantWise-SUS-Questionnaire.docx");
Packer.toBuffer(doc).then((buf) => { fs.writeFileSync(outPath, buf); console.log("WROTE " + outPath); });
