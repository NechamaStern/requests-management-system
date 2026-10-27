# Client

אפליקציית Angular 21 שנוצרה באמצעות Angular CLI. נדרש Node.js 22.12 ומעלה בענף 22, או 24 ומעלה.

```powershell
Push-Location .\client
npm ci
npm start
Pop-Location
```

האפליקציה זמינה ב-`http://localhost:4200`. בזמן פיתוח, בקשות ל-`/api` מועברות לשרת ב-`http://localhost:5231`; יש להפעיל אותו בנפרד לפי ההוראות שב-README הראשי.

פקודות שימושיות מתוך תיקיית `client`:

```powershell
npm run build
npm test
```
