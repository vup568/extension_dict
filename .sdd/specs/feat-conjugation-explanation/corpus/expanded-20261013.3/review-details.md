# F-09 .3 — Chi tiết phần thay đổi

Các ca không đổi giữ đáp án/thành phần trong [review-details .2](../expanded-20261013.2/review-details.md); dữ liệu đầy đủ để test nằm ở [cases.json](./cases.json). Đây là đề xuất kiểm duyệt, không phải output runtime.

## V11-P1: 食べられる

Kết quả dự kiến: supported. 
- conjugation:verb-passive@1; dạng gốc 食べる. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần bị động trong cách hiểu này. → る [4,5): Đuôi không quá khứ của dạng đang được phân tích.

- conjugation:verb-potential@1; dạng gốc 食べる. Thể khả năng: có thể thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần khả năng trong cách hiểu này. → る [4,5): Đuôi không quá khứ của dạng đang được phân tích.

- conjugation:verb-honorific@1; dạng gốc 食べる. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → る [4,5): Đuôi không quá khứ của dạng đang được phân tích.


## V11-P2: 書かれる

Kết quả dự kiến: supported. 
- conjugation:verb-passive@1; dạng gốc 書く. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 書く.

  Thành phần: 書か [0,2): Phần động từ của 書く ở dạng quan sát này. → れ [2,3): Phần bị động trong cách hiểu này. → る [3,4): Đuôi không quá khứ của dạng đang được phân tích.

- conjugation:verb-honorific@1; dạng gốc 書く. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 書く.

  Thành phần: 書か [0,2): Phần động từ của 書く ở dạng quan sát này. → れ [2,3): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → る [3,4): Đuôi không quá khứ của dạng đang được phân tích.


## V13-P1: 食べさせられる

Kết quả dự kiến: supported. 
- conjugation:verb-causative-passive@1; dạng gốc 食べる. Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → させ [2,4): Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. → られ [4,6): Phần bị động trong cách hiểu này. → る [6,7): Đuôi không quá khứ của dạng đang được phân tích.

- conjugation:verb-causative-potential@1; dạng gốc 食べる. Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → させ [2,4): Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu. → られ [4,6): Phần khả năng của dạng sai khiến trong cách hiểu này. → る [6,7): Đuôi không quá khứ của dạng đang được phân tích.


## V11-C-plain_past-P1: 食べられた

Kết quả dự kiến: supported. 
- conjugation:verb-passive:plain-past@1; dạng gốc 食べる. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần bị động trong cách hiểu này. → た [4,5): Đánh dấu quá khứ.

- conjugation:verb-potential:plain-past@1; dạng gốc 食べる. Thể khả năng: có thể thực hiện hành động; quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần khả năng trong cách hiểu này. → た [4,5): Đánh dấu quá khứ.

- conjugation:verb-honorific:plain-past@1; dạng gốc 食べる. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → た [4,5): Đánh dấu quá khứ.


## V11-C-plain_past-P2: 書かれた

Kết quả dự kiến: supported. 
- conjugation:verb-passive:plain-past@1; dạng gốc 書く. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; quá khứ, thông thường. Dạng gốc: 書く.

  Thành phần: 書か [0,2): Phần động từ của 書く ở dạng quan sát này. → れ [2,3): Phần bị động trong cách hiểu này. → た [3,4): Đánh dấu quá khứ.

- conjugation:verb-honorific:plain-past@1; dạng gốc 書く. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; quá khứ, thông thường. Dạng gốc: 書く.

  Thành phần: 書か [0,2): Phần động từ của 書く ở dạng quan sát này. → れ [2,3): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → た [3,4): Đánh dấu quá khứ.


## V11-C-plain_negative-P1: 食べられない

Kết quả dự kiến: supported. 
- conjugation:verb-passive:plain-negative@1; dạng gốc 食べる. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; phủ định, không quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần bị động trong cách hiểu này. → ない [4,6): Phủ định, không quá khứ.

- conjugation:verb-potential:plain-negative@1; dạng gốc 食べる. Thể khả năng: có thể thực hiện hành động; phủ định, không quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần khả năng trong cách hiểu này. → ない [4,6): Phủ định, không quá khứ.

- conjugation:verb-honorific:plain-negative@1; dạng gốc 食べる. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; phủ định, không quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → ない [4,6): Phủ định, không quá khứ.


## V11-C-plain_negative-P2: 書かれない

Kết quả dự kiến: supported. 
- conjugation:verb-passive:plain-negative@1; dạng gốc 書く. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; phủ định, không quá khứ, thông thường. Dạng gốc: 書く.

  Thành phần: 書か [0,2): Phần động từ của 書く ở dạng quan sát này. → れ [2,3): Phần bị động trong cách hiểu này. → ない [3,5): Phủ định, không quá khứ.

- conjugation:verb-honorific:plain-negative@1; dạng gốc 書く. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; phủ định, không quá khứ, thông thường. Dạng gốc: 書く.

  Thành phần: 書か [0,2): Phần động từ của 書く ở dạng quan sát này. → れ [2,3): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → ない [3,5): Phủ định, không quá khứ.


## V11-C-plain_negative_past-P1: 食べられなかった

Kết quả dự kiến: supported. 
- conjugation:verb-passive:plain-negative-past@1; dạng gốc 食べる. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; phủ định, quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần bị động trong cách hiểu này. → なかっ [4,7): Phần phủ định nối với đuôi quá khứ. → た [7,8): Đánh dấu quá khứ.

- conjugation:verb-potential:plain-negative-past@1; dạng gốc 食べる. Thể khả năng: có thể thực hiện hành động; phủ định, quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần khả năng trong cách hiểu này. → なかっ [4,7): Phần phủ định nối với đuôi quá khứ. → た [7,8): Đánh dấu quá khứ.

- conjugation:verb-honorific:plain-negative-past@1; dạng gốc 食べる. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; phủ định, quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → なかっ [4,7): Phần phủ định nối với đuôi quá khứ. → た [7,8): Đánh dấu quá khứ.


## V11-C-plain_negative_past-P2: 書かれなかった

Kết quả dự kiến: supported. 
- conjugation:verb-passive:plain-negative-past@1; dạng gốc 書く. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; phủ định, quá khứ, thông thường. Dạng gốc: 書く.

  Thành phần: 書か [0,2): Phần động từ của 書く ở dạng quan sát này. → れ [2,3): Phần bị động trong cách hiểu này. → なかっ [3,6): Phần phủ định nối với đuôi quá khứ. → た [6,7): Đánh dấu quá khứ.

- conjugation:verb-honorific:plain-negative-past@1; dạng gốc 書く. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; phủ định, quá khứ, thông thường. Dạng gốc: 書く.

  Thành phần: 書か [0,2): Phần động từ của 書く ở dạng quan sát này. → れ [2,3): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → なかっ [3,6): Phần phủ định nối với đuôi quá khứ. → た [6,7): Đánh dấu quá khứ.


## V11-C-polite_nonpast-P1: 食べられます

Kết quả dự kiến: supported. 
- conjugation:verb-passive:polite-nonpast@1; dạng gốc 食べる. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, lịch sự. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần bị động trong cách hiểu này. → ます [4,6): Lịch sự, không quá khứ.

- conjugation:verb-potential:polite-nonpast@1; dạng gốc 食べる. Thể khả năng: có thể thực hiện hành động; không quá khứ, lịch sự. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần khả năng trong cách hiểu này. → ます [4,6): Lịch sự, không quá khứ.

- conjugation:verb-honorific:polite-nonpast@1; dạng gốc 食べる. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; không quá khứ, lịch sự. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → ます [4,6): Lịch sự, không quá khứ.


## V11-C-polite_nonpast-P2: 書かれます

Kết quả dự kiến: supported. 
- conjugation:verb-passive:polite-nonpast@1; dạng gốc 書く. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, lịch sự. Dạng gốc: 書く.

  Thành phần: 書か [0,2): Phần động từ của 書く ở dạng quan sát này. → れ [2,3): Phần bị động trong cách hiểu này. → ます [3,5): Lịch sự, không quá khứ.

- conjugation:verb-honorific:polite-nonpast@1; dạng gốc 書く. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; không quá khứ, lịch sự. Dạng gốc: 書く.

  Thành phần: 書か [0,2): Phần động từ của 書く ở dạng quan sát này. → れ [2,3): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → ます [3,5): Lịch sự, không quá khứ.


## V11-C-polite_past-P1: 食べられました

Kết quả dự kiến: supported. 
- conjugation:verb-passive:polite-past@1; dạng gốc 食べる. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; quá khứ, lịch sự. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần bị động trong cách hiểu này. → まし [4,6): Phần lịch sự nối với quá khứ. → た [6,7): Đánh dấu quá khứ.

- conjugation:verb-potential:polite-past@1; dạng gốc 食べる. Thể khả năng: có thể thực hiện hành động; quá khứ, lịch sự. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần khả năng trong cách hiểu này. → まし [4,6): Phần lịch sự nối với quá khứ. → た [6,7): Đánh dấu quá khứ.

- conjugation:verb-honorific:polite-past@1; dạng gốc 食べる. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; quá khứ, lịch sự. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → まし [4,6): Phần lịch sự nối với quá khứ. → た [6,7): Đánh dấu quá khứ.


## V11-C-polite_past-P2: 書かれました

Kết quả dự kiến: supported. 
- conjugation:verb-passive:polite-past@1; dạng gốc 書く. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; quá khứ, lịch sự. Dạng gốc: 書く.

  Thành phần: 書か [0,2): Phần động từ của 書く ở dạng quan sát này. → れ [2,3): Phần bị động trong cách hiểu này. → まし [3,5): Phần lịch sự nối với quá khứ. → た [5,6): Đánh dấu quá khứ.

- conjugation:verb-honorific:polite-past@1; dạng gốc 書く. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; quá khứ, lịch sự. Dạng gốc: 書く.

  Thành phần: 書か [0,2): Phần động từ của 書く ở dạng quan sát này. → れ [2,3): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → まし [3,5): Phần lịch sự nối với quá khứ. → た [5,6): Đánh dấu quá khứ.


## V11-C-polite_negative-P1: 食べられません

Kết quả dự kiến: supported. 
- conjugation:verb-passive:polite-negative@1; dạng gốc 食べる. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; phủ định, không quá khứ, lịch sự. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần bị động trong cách hiểu này. → ませ [4,6): Phần lịch sự nối với phủ định. → ん [6,7): Phủ định trong chuỗi lịch sự.

- conjugation:verb-potential:polite-negative@1; dạng gốc 食べる. Thể khả năng: có thể thực hiện hành động; phủ định, không quá khứ, lịch sự. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần khả năng trong cách hiểu này. → ませ [4,6): Phần lịch sự nối với phủ định. → ん [6,7): Phủ định trong chuỗi lịch sự.

- conjugation:verb-honorific:polite-negative@1; dạng gốc 食べる. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; phủ định, không quá khứ, lịch sự. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → ませ [4,6): Phần lịch sự nối với phủ định. → ん [6,7): Phủ định trong chuỗi lịch sự.


## V11-C-polite_negative-P2: 書かれません

Kết quả dự kiến: supported. 
- conjugation:verb-passive:polite-negative@1; dạng gốc 書く. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; phủ định, không quá khứ, lịch sự. Dạng gốc: 書く.

  Thành phần: 書か [0,2): Phần động từ của 書く ở dạng quan sát này. → れ [2,3): Phần bị động trong cách hiểu này. → ませ [3,5): Phần lịch sự nối với phủ định. → ん [5,6): Phủ định trong chuỗi lịch sự.

- conjugation:verb-honorific:polite-negative@1; dạng gốc 書く. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; phủ định, không quá khứ, lịch sự. Dạng gốc: 書く.

  Thành phần: 書か [0,2): Phần động từ của 書く ở dạng quan sát này. → れ [2,3): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → ませ [3,5): Phần lịch sự nối với phủ định. → ん [5,6): Phủ định trong chuỗi lịch sự.


## V11-C-polite_negative_past-P1: 食べられませんでした

Kết quả dự kiến: supported. 
- conjugation:verb-passive:polite-negative-past@1; dạng gốc 食べる. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; phủ định, quá khứ, lịch sự. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần bị động trong cách hiểu này. → ませ [4,6): Phần lịch sự nối với phủ định. → ん [6,7): Phủ định trong chuỗi lịch sự. → でし [7,9): Phần lịch sự nối với quá khứ sau phủ định. → た [9,10): Đánh dấu quá khứ.

- conjugation:verb-potential:polite-negative-past@1; dạng gốc 食べる. Thể khả năng: có thể thực hiện hành động; phủ định, quá khứ, lịch sự. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần khả năng trong cách hiểu này. → ませ [4,6): Phần lịch sự nối với phủ định. → ん [6,7): Phủ định trong chuỗi lịch sự. → でし [7,9): Phần lịch sự nối với quá khứ sau phủ định. → た [9,10): Đánh dấu quá khứ.

- conjugation:verb-honorific:polite-negative-past@1; dạng gốc 食べる. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; phủ định, quá khứ, lịch sự. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → ませ [4,6): Phần lịch sự nối với phủ định. → ん [6,7): Phủ định trong chuỗi lịch sự. → でし [7,9): Phần lịch sự nối với quá khứ sau phủ định. → た [9,10): Đánh dấu quá khứ.


## V11-C-polite_negative_past-P2: 書かれませんでした

Kết quả dự kiến: supported. 
- conjugation:verb-passive:polite-negative-past@1; dạng gốc 書く. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; phủ định, quá khứ, lịch sự. Dạng gốc: 書く.

  Thành phần: 書か [0,2): Phần động từ của 書く ở dạng quan sát này. → れ [2,3): Phần bị động trong cách hiểu này. → ませ [3,5): Phần lịch sự nối với phủ định. → ん [5,6): Phủ định trong chuỗi lịch sự. → でし [6,8): Phần lịch sự nối với quá khứ sau phủ định. → た [8,9): Đánh dấu quá khứ.

- conjugation:verb-honorific:polite-negative-past@1; dạng gốc 書く. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; phủ định, quá khứ, lịch sự. Dạng gốc: 書く.

  Thành phần: 書か [0,2): Phần động từ của 書く ở dạng quan sát này. → れ [2,3): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → ませ [3,5): Phần lịch sự nối với phủ định. → ん [5,6): Phủ định trong chuỗi lịch sự. → でし [6,8): Phần lịch sự nối với quá khứ sau phủ định. → た [8,9): Đánh dấu quá khứ.


## V11-IRR-suru: される

Kết quả dự kiến: supported. 
- conjugation:verb-passive@1; dạng gốc する. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: する.

  Thành phần: さ [0,1): Phần động từ của する ở dạng quan sát này. → れ [1,2): Phần bị động trong cách hiểu này. → る [2,3): Đuôi không quá khứ của dạng đang được phân tích.

- conjugation:verb-honorific@1; dạng gốc する. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: する.

  Thành phần: さ [0,1): Phần động từ của する ở dạng quan sát này. → れ [1,2): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → る [2,3): Đuôi không quá khứ của dạng đang được phân tích.


## V11-IRR-kuru: 来られる

Kết quả dự kiến: supported. 
- conjugation:verb-passive@1; dạng gốc 来る. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 来る.

  Thành phần: 来 [0,1): Phần động từ của 来る ở dạng quan sát này. → られ [1,3): Phần bị động trong cách hiểu này. → る [3,4): Đuôi không quá khứ của dạng đang được phân tích.

- conjugation:verb-potential@1; dạng gốc 来る. Thể khả năng: có thể thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 来る.

  Thành phần: 来 [0,1): Phần động từ của 来る ở dạng quan sát này. → られ [1,3): Phần khả năng trong cách hiểu này. → る [3,4): Đuôi không quá khứ của dạng đang được phân tích.

- conjugation:verb-honorific@1; dạng gốc 来る. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 来る.

  Thành phần: 来 [0,1): Phần động từ của 来る ở dạng quan sát này. → られ [1,3): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → る [3,4): Đuôi không quá khứ của dạng đang được phân tích.


## V10-IRR-kuru: 来られる

Kết quả dự kiến: supported. 
- conjugation:verb-passive@1; dạng gốc 来る. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 来る.

  Thành phần: 来 [0,1): Phần động từ của 来る ở dạng quan sát này. → られ [1,3): Phần bị động trong cách hiểu này. → る [3,4): Đuôi không quá khứ của dạng đang được phân tích.

- conjugation:verb-potential@1; dạng gốc 来る. Thể khả năng: có thể thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 来る.

  Thành phần: 来 [0,1): Phần động từ của 来る ở dạng quan sát này. → られ [1,3): Phần khả năng trong cách hiểu này. → る [3,4): Đuôi không quá khứ của dạng đang được phân tích.

- conjugation:verb-honorific@1; dạng gốc 来る. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 来る.

  Thành phần: 来 [0,1): Phần động từ của 来る ở dạng quan sát này. → られ [1,3): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → る [3,4): Đuôi không quá khứ của dạng đang được phân tích.


## AMB-01: 食べられる

Kết quả dự kiến: supported. 
- conjugation:verb-passive@1; dạng gốc 食べる. Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần bị động trong cách hiểu này. → る [4,5): Đuôi không quá khứ của dạng đang được phân tích.

- conjugation:verb-potential@1; dạng gốc 食べる. Thể khả năng: có thể thực hiện hành động; không quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần khả năng trong cách hiểu này. → る [4,5): Đuôi không quá khứ của dạng đang được phân tích.

- conjugation:verb-honorific@1; dạng gốc 食べる. Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. không mặc định là bất lợi; không quá khứ, thông thường. Dạng gốc: 食べる.

  Thành phần: 食べ [0,2): Phần động từ của 食べる ở dạng quan sát này. → られ [2,4): Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động. → る [4,5): Đuôi không quá khứ của dạng đang được phân tích.


## V21-EXCLUDED-1: できられる

Kết quả dự kiến: unavailable. 
Lý do loại: Do not generate a honorific by attaching rareru to a potential verb.


## V21-EXCLUDED-2: できられた

Kết quả dự kiến: unavailable. 
Lý do loại: Do not generate a honorific by attaching rareru to a potential verb.


## V21-EXCLUDED-3: 書けられる

Kết quả dự kiến: unavailable. 
Lý do loại: Do not generate a honorific by attaching rareru to a potential verb.


## V21-EXCLUDED-4: 書けられた

Kết quả dự kiến: unavailable. 
Lý do loại: Do not generate a honorific by attaching rareru to a potential verb.


