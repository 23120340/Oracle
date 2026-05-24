# Talking Points

| Cau hoi | Tra loi ngan |
| --- | --- |
| Vi sao KTV dung RBAC/view thay vi VPD? | KTV chi can loc theo `MAKTV`; view co trigger la du, de demo ro va de kiem soat cot update. |
| Vi sao BS dung VPD? | BS truy cap cung bang `HSBA`, `HSBA_DV`, `DONTHUOC`; VPD tu them predicate nen van an toan khi truy cap SQL truc tiep. |
| `update_check => TRUE` trong VPD co tac dung gi? | Sau update Oracle kiem tra lai policy, tranh viec sua khoa loc de chuyen dong du lieu sang pham vi khac. |
| OLS compartment va group khac nhau the nao? | Compartment co nghia AND, user phai co tat ca compartment cua dong; group co nghia OR va co phan cap. |
| Vi sao mask CCCD o grid? | Grid hien thi nhieu dong nen de lo thong tin khi nguoi khac nhin man hinh; form chi tiet la ngu canh co chu dich hon. |
| Brute-force app layer khac DB profile the nao? | App chan som truoc khi gui ket noi that bai lien tuc xuong DB; DB profile van la lop bao ve cuoi. |
| Vi sao can Oracle Net encryption? | RBAC/VPD/OLS bao ve trong database, con Oracle Net encryption bao ve du lieu tren duong truyen TCP. |
