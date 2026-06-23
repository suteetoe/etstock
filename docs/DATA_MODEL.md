# ETStock — Data Model (PostgreSQL)

โครงสร้างข้อมูลเริ่มต้นสำหรับระบบออกใบกำกับภาษีอย่างย่อ
(เป็นแบบร่าง ปรับได้ตอนพัฒนาจริง)

## แผนภาพความสัมพันธ์ (โดยย่อ)

```
companies ─┐
           │ (ผู้ออกใบกำกับ)
products ──┴─< monthly_transactions >── period (เดือน/ปี)
   │                                        │
   └──< stock_balances (ยกมา/ยกไป)          │
                                            │
abbreviated_invoices >── invoice_items ─────┘
```

## ตาราง (Tables)

### companies — ข้อมูลบริษัทผู้ออกใบกำกับ
| คอลัมน์ | ชนิด | หมายเหตุ |
|---------|------|----------|
| id | bigserial PK | |
| name | text | ชื่อบริษัท |
| tax_id | varchar(13) | เลขประจำตัวผู้เสียภาษี |
| branch | varchar(20) | สาขา (เช่น 00000) |
| address | text | ที่อยู่ |
| vat_rate | numeric(5,2) | อัตราภาษี เช่น 7.00 |
| invoice_prefix | text | prefix เลขที่ใบกำกับ |
| created_at / updated_at | timestamptz | |

### products — สินค้า
| คอลัมน์ | ชนิด | หมายเหตุ |
|---------|------|----------|
| id | bigserial PK | |
| code | text UNIQUE | รหัสสินค้า |
| name | text | ชื่อสินค้า |
| unit | text | หน่วยนับ |
| cost | numeric(12,2) | ต้นทุนล่าสุด |
| price | numeric(12,2) | ราคาขาย (รวม VAT) |
| is_active | boolean | |
| created_at / updated_at | timestamptz | |

### stock_balances — ยอดคงเหลือยกมา/ยกไป ต่อสินค้า ต่อเดือน
| คอลัมน์ | ชนิด | หมายเหตุ |
|---------|------|----------|
| id | bigserial PK | |
| product_id | bigint FK → products | |
| period | date | วันที่ 1 ของเดือน (เช่น 2026-06-01) |
| qty_forward | numeric(14,3) | คงเหลือยกมา |
| cost | numeric(12,2) | ต้นทุนยกมา |
| qty_carry | numeric(14,3) | คงเหลือยกไป (คำนวณ) |
| UNIQUE | (product_id, period) | |

### monthly_transactions — จำนวนซื้อ/ขายต่อสินค้า ต่อเดือน
| คอลัมน์ | ชนิด | หมายเหตุ |
|---------|------|----------|
| id | bigserial PK | |
| product_id | bigint FK → products | |
| period | date | เดือน/ปี |
| qty_purchase | numeric(14,3) | จำนวนซื้อ |
| qty_sale_full | numeric(14,3) | ขาย (ใบกำกับเต็ม) |
| qty_sale_pos | numeric(14,3) | ขายหน้าร้าน (ใบกำกับย่อ) |
| UNIQUE | (product_id, period) | |

### abbreviated_invoices — หัวใบกำกับภาษีอย่างย่อ
| คอลัมน์ | ชนิด | หมายเหตุ |
|---------|------|----------|
| id | bigserial PK | |
| company_id | bigint FK → companies | |
| period | date | เดือน/ปีที่สร้าง |
| invoice_no | text | เลขที่ใบกำกับ (running) |
| issued_at | timestamptz | วันเวลาออก |
| total_amount | numeric(14,2) | มูลค่ารวม (รวม VAT) |
| vat_amount | numeric(14,2) | ภาษีมูลค่าเพิ่ม |
| UNIQUE | (company_id, invoice_no) | |

### invoice_items — รายการสินค้าในใบกำกับ
| คอลัมน์ | ชนิด | หมายเหตุ |
|---------|------|----------|
| id | bigserial PK | |
| invoice_id | bigint FK → abbreviated_invoices (ON DELETE CASCADE) | |
| product_id | bigint FK → products | |
| qty | numeric(14,3) | จำนวน |
| unit_price | numeric(12,2) | ราคา/หน่วย (รวม VAT) |
| amount | numeric(14,2) | รวม |

## ดัชนีที่แนะนำ (Indexes)
- `stock_balances (period)`, `monthly_transactions (period)`
- `abbreviated_invoices (company_id, period)`
- `invoice_items (invoice_id)`

## หมายเหตุการคำนวณ
- `qty_carry = qty_forward + qty_purchase − qty_sale_full − qty_sale_pos`
- การสร้างใบกำกับย่อของเดือนหนึ่งจะลบ `abbreviated_invoices` เดิมของ (company, period)
  แบบ CASCADE ลง `invoice_items` ก่อนสร้างชุดใหม่ (หลังผู้ใช้ยืนยัน)
