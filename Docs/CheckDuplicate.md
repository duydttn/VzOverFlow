# 🔍 CheckDuplicate Documentation

Hệ thống VzOverFlow sử dụng trí tuệ nhân tạo (AI) để tự động phát hiện các câu hỏi trùng lặp hoặc có nội dung tương tự, giúp duy trì kho kiến thức cô đọng và tránh loãng thông tin.

## 🚀 Công nghệ sử dụng

Logic kiểm tra trùng lặp được triển khai tại `Services/QuestionService.cs` (phương thức `FindSimilarQuestionsAsync`), kết hợp hai kỹ thuật tiên tiến:

### 1. Gemini Embeddings
- Khi một câu hỏi được tạo (hoặc khi kiểm tra trùng lặp), hệ thống gửi toàn bộ tiêu đề (Title) và nội dung (Body) đến **Google Gemini AI**.
- Gemini sẽ chuyển đổi văn bản sang một **Vector** (mảng các số thực) đại diện cho ý nghĩa ngữ nghĩa (Semantic Meaning) của câu hỏi.
- Điều này cho phép hệ thống hiểu được hai câu hỏi giống nhau về ý nghĩa ngay cả khi chúng dùng từ ngữ khác nhau.

### 2. Cosine Similarity (Độ tương đồng Cosin)
- Sau khi có vector, hệ thống so sánh nó với toàn bộ cơ sở dữ liệu câu hỏi đã có.
- Công thức **Cosine Similarity** được sử dụng để tính toán góc giữa hai vector trong không gian đa chiều.
- Giá trị trả về nằm trong khoảng từ 0 đến 1.

## 🎯 Logic xử lý

### Ngưỡng chặn (Threshold)
Hệ thống sử dụng ngưỡng **0.8 (80%)**:
- **Similarity > 0.8**: Được coi là "Rất giống" hoặc "Trùng lặp".
- Hệ thống sẽ lấy ra tối đa **5 câu hỏi tương tự nhất** để gợi ý cho người dùng.

### Quy trình hoạt động
1. **Input**: Người dùng nhập tiêu đề và nội dung câu hỏi mới.
2. **Feature Extraction**: Hệ thống tạo embedding vector cho input này.
3. **Comparison**: Chạy thuật toán so sánh với `EmbeddingJson` của các câu hỏi trong database.
4. **Ranking**: Sắp xếp theo độ tương đồng giảm dần.
5. **Output**: Trả về danh sách câu hỏi có độ tương đồng trên 0.8.

## 🛠️ Triển khai trong Code

```csharp
public async Task<List<Question>> FindSimilarQuestionsAsync(string title, string body)
{
    // 1. Lấy tất cả câu hỏi đã có vector
    var allQuestions = await _context.Questions.Where(q => q.EmbeddingJson != null).ToListAsync();
    
    // 2. Tạo vector cho nội dung mới
    var inputEmbedding = await _geminiService.GetEmbeddingAsync($"{title} {body}");
    
    // 3. Tính toán độ tương đồng và lọc theo ngưỡng 0.8
    var similar = allQuestions
        .Select(q => new { 
            Question = q, 
            Similarity = CosineSimilarity(inputEmbedding, JsonSerializer.Deserialize<float[]>(q.EmbeddingJson)) 
        })
        .Where(x => x.Similarity > 0.8)
        .OrderByDescending(x => x.Similarity)
        .Take(5)
        .ToList();
        
    return similar.Select(x => x.Question).ToList();
}
```

---
> [!IMPORTANT]
> Cơ chế này hiệu quả hơn nhiều so với việc chỉ tìm kiếm từ khóa (Keyword match) thông thường vì nó hiểu được văn cảnh và mục tiêu của câu hỏi.
