# HasDiscount Filter Implementation

## Tóm tắt thay đổi

### 1. Model Updates
- `AffiliateProductFilterRequest` đã có field `HasDiscount` (bool?)

### 2. Business Logic Implementation

#### Thêm filter logic trong `AffiliateBusiness.GetProductsAsync()`
```csharp
if (request.HasDiscount.HasValue)
{
    if (request.HasDiscount.Value)
    {
        // hasDiscount=true: Chỉ sản phẩm có giảm giá
        query = query.Where(p => p.DiscountPrice.HasValue && p.DiscountPrice < p.Price);
    }
    else
    {
        // hasDiscount=false: Chỉ sản phẩm không có giảm giá
        query = query.Where(p => !p.DiscountPrice.HasValue || p.DiscountPrice >= p.Price);
    }
}
```

### 3. API Documentation Updates

#### Thêm parameter mới:
- `hasDiscount`: true | false - Filter sản phẩm có/không có giảm giá

#### Example URL:
```
GET /api/affiliate/products?hasDiscount=true&attributes=Màu%20sắc:Đỏ
```

## Logic xử lý

### hasDiscount=true (Có giảm giá)
- `DiscountPrice` phải có giá trị (not null)
- `DiscountPrice` phải nhỏ hơn `Price` gốc
- Điều kiện: `p.DiscountPrice.HasValue && p.DiscountPrice < p.Price`

### hasDiscount=false (Không có giảm giá)
- `DiscountPrice` là null HOẶC
- `DiscountPrice` >= `Price` (không thực sự giảm giá)
- Điều kiện: `!p.DiscountPrice.HasValue || p.DiscountPrice >= p.Price`

### hasDiscount=null (Không filter)
- Trả về tất cả sản phẩm (có và không có giảm giá)

## Test Results

### Sample Data:
1. **Vòng tay thạch anh đỏ may mắn**: Price=299000, DiscountPrice=199000 (HasDiscount=true)
2. **Tiền vàng phong thủy may mắn**: Price=150000, DiscountPrice=null (HasDiscount=false)

### Test Cases:

#### 1. hasDiscount=true
```
URL: /products?hasDiscount=true
Result: 1 sản phẩm (Vòng tay thạch anh đỏ may mắn)
```

#### 2. hasDiscount=false  
```
URL: /products?hasDiscount=false
Result: 1 sản phẩm (Tiền vàng phong thủy may mắn)
```

#### 3. Combine với attribute filter
```
URL: /products?hasDiscount=true&attributes=Màu%20sắc:Đỏ
Result: 1 sản phẩm (thỏa mãn cả 2 điều kiện)
```

#### 4. Edge case - No results
```
URL: /products?hasDiscount=false&attributes=Màu%20sắc:Đỏ
Result: 0 sản phẩm (không có sản phẩm nào màu đỏ mà không giảm giá)
```

## Frontend Integration

### Query String Building
```javascript
const filters = {
  hasDiscount: true,
  attributes: ['Màu sắc:Đỏ'],
  sortBy: 'price_asc'
};

const queryString = new URLSearchParams();
if (filters.hasDiscount !== undefined) {
  queryString.append('hasDiscount', filters.hasDiscount);
}
```

### Filter UI
```javascript
// Checkbox cho discount filter
<input 
  type="checkbox" 
  checked={hasDiscountFilter} 
  onChange={(e) => setHasDiscountFilter(e.target.checked ? true : undefined)}
/>
<label>Chỉ sản phẩm giảm giá</label>
```

## Database Query Performance

### Efficient Filtering
- Filter được thực hiện ở database level (không phải in-memory)
- Sử dụng indexed columns (`DiscountPrice`, `Price`)
- Kết hợp tốt với các filters khác (attributes, categories, etc.)

### Query Example
```sql
SELECT * FROM "AffiliateProducts" 
WHERE "Status" = 1 
  AND "Stock" > 0
  AND "DiscountPrice" IS NOT NULL 
  AND "DiscountPrice" < "Price"  -- hasDiscount=true
```

✅ **Implementation Complete**: Filter hoạt động chính xác, combine tốt với các filters khác, performance tối ưu!
