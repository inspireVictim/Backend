#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Упрощенный скрипт для конвертации Markdown в DOCX
Использует только стандартные библиотеки Python
"""

import re
import zipfile
from pathlib import Path
from xml.etree.ElementTree import Element, SubElement, tostring
from xml.dom import minidom

def create_docx_from_markdown(md_file_path, docx_file_path):
    """Создает DOCX файл из Markdown используя только стандартные библиотеки"""
    
    # Читаем MD файл
    with open(md_file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Создаем структуру DOCX (упрощенную)
    # DOCX - это ZIP архив с XML файлами
    
    # Создаем временную директорию
    temp_dir = Path('temp_docx')
    temp_dir.mkdir(exist_ok=True)
    
    # Создаем [Content_Types].xml
    content_types = Element('Types', xmlns='http://schemas.openxmlformats.org/package/2006/content-types')
    
    # Добавляем типы
    for ext in ['xml', 'rels', 'png', 'jpeg', 'jpg']:
        default = SubElement(content_types, 'Default')
        default.set('Extension', ext)
        if ext in ['xml', 'rels']:
            default.set('ContentType', f'application/xml')
        else:
            default.set('ContentType', f'image/{ext}')
    
    override = SubElement(content_types, 'Override')
    override.set('PartName', '/word/document.xml')
    override.set('ContentType', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml')
    
    # Сохраняем [Content_Types].xml
    with open(temp_dir / '[Content_Types].xml', 'w', encoding='utf-8') as f:
        f.write(prettify(content_types))
    
    # Создаем word/document.xml
    document = Element('w:document', xmlns_w='http://schemas.openxmlformats.org/wordprocessingml/2006/main')
    body = SubElement(document, 'w:body')
    
    # Парсим Markdown
    lines = content.split('\n')
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        
        if not line:
            i += 1
            continue
        
        # Заголовки
        if line.startswith('# '):
            p = SubElement(body, 'w:p')
            r = SubElement(p, 'w:r')
            t = SubElement(r, 'w:t')
            t.text = line[2:].strip()
            ppr = SubElement(p, 'w:pPr')
            pstyle = SubElement(ppr, 'w:pStyle')
            pstyle.set('w:val', 'Heading1')
        elif line.startswith('## '):
            p = SubElement(body, 'w:p')
            r = SubElement(p, 'w:r')
            t = SubElement(r, 'w:t')
            t.text = line[3:].strip()
            ppr = SubElement(p, 'w:pPr')
            pstyle = SubElement(ppr, 'w:pStyle')
            pstyle.set('w:val', 'Heading2')
        elif line.startswith('### '):
            p = SubElement(body, 'w:p')
            r = SubElement(p, 'w:r')
            t = SubElement(r, 'w:t')
            t.text = line[4:].strip()
            ppr = SubElement(p, 'w:pPr')
            pstyle = SubElement(ppr, 'w:pStyle')
            pstyle.set('w:val', 'Heading3')
        # Списки
        elif line.startswith('- '):
            p = SubElement(body, 'w:p')
            ppr = SubElement(p, 'w:pPr')
            numpr = SubElement(ppr, 'w:numPr')
            ilvl = SubElement(numpr, 'w:ilvl')
            ilvl.set('w:val', '0')
            numid = SubElement(numpr, 'w:numId')
            numid.set('w:val', '1')
            r = SubElement(p, 'w:r')
            t = SubElement(r, 'w:t')
            t.text = line[2:].strip()
        # Обычный текст
        else:
            # Убираем markdown форматирование
            text = re.sub(r'\*\*(.*?)\*\*', r'\1', line)  # Жирный
            text = re.sub(r'`(.*?)`', r'\1', text)  # Код
            text = re.sub(r'\[(.*?)\]\(.*?\)', r'\1', text)  # Ссылки
            
            p = SubElement(body, 'w:p')
            r = SubElement(p, 'w:r')
            t = SubElement(r, 'w:t')
            t.text = text
        
        i += 1
    
    # Сохраняем document.xml
    word_dir = temp_dir / 'word'
    word_dir.mkdir(exist_ok=True)
    with open(word_dir / 'document.xml', 'w', encoding='utf-8') as f:
        f.write(prettify(document))
    
    # Создаем _rels/.rels
    rels_dir = temp_dir / '_rels'
    rels_dir.mkdir(exist_ok=True)
    relationships = Element('Relationships', xmlns='http://schemas.openxmlformats.org/package/2006/relationships')
    rel = SubElement(relationships, 'Relationship')
    rel.set('Id', 'rId1')
    rel.set('Type', 'http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument')
    rel.set('Target', 'word/document.xml')
    
    with open(rels_dir / '.rels', 'w', encoding='utf-8') as f:
        f.write(prettify(relationships))
    
    # Создаем ZIP архив (DOCX)
    with zipfile.ZipFile(docx_file_path, 'w', zipfile.ZIP_DEFLATED) as zipf:
        zipf.write(temp_dir / '[Content_Types].xml', '[Content_Types].xml')
        zipf.write(temp_dir / 'word' / 'document.xml', 'word/document.xml')
        zipf.write(temp_dir / '_rels' / '.rels', '_rels/.rels')
    
    # Удаляем временную директорию
    import shutil
    shutil.rmtree(temp_dir)
    
    print(f"✓ DOCX файл создан: {docx_file_path}")

def prettify(elem):
    """Форматирует XML элемент"""
    rough_string = tostring(elem, encoding='unicode')
    reparsed = minidom.parseString(rough_string)
    return reparsed.toprettyxml(indent="  ")

def create_zip_archive(docx_file_path, zip_file_path):
    """Создает ZIP архив с DOCX файлом"""
    
    with zipfile.ZipFile(zip_file_path, 'w', zipfile.ZIP_DEFLATED) as zipf:
        zipf.write(docx_file_path, Path(docx_file_path).name)
    
    print(f"✓ ZIP архив создан: {zip_file_path}")

def main():
    """Главная функция"""
    
    md_file = Path('BACKEND_DEVELOPER_KNOWLEDGE.md')
    docx_file = Path('BACKEND_DEVELOPER_KNOWLEDGE.docx')
    zip_file = Path('BACKEND_DEVELOPER_KNOWLEDGE.zip')
    
    if not md_file.exists():
        print(f"❌ Ошибка: файл {md_file} не найден!")
        return
    
    try:
        print("🔄 Конвертация MD в DOCX...")
        create_docx_from_markdown(md_file, docx_file)
        
        print("🔄 Создание ZIP архива...")
        create_zip_archive(docx_file, zip_file)
        
        print("\n✅ Конвертация завершена успешно!")
        print(f"📄 DOCX файл: {docx_file.absolute()}")
        print(f"📦 ZIP архив: {zip_file.absolute()}")
        
    except Exception as e:
        print(f"❌ Ошибка: {e}")
        import traceback
        traceback.print_exc()

if __name__ == '__main__':
    main()

