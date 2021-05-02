#! /usr/bin/env python3

import argparse
import sys, os
import re
import json
import collections
import datetime

PATH = os.path.abspath(os.path.dirname(__file__))


class Translation:
    def __init__(self, language):
        self.language = language
        self.keys = {}

    def __str__(self):
        result = "For language '%s':" % (self.language)
        for ctx in self.ctx_list():
            for source in self.sources_list(ctx):
                translation = self.translation(ctx, source)
                is_used = self.is_used(ctx, source)
                special_characters_check = self.special_characters_check(source, translation)
                result += '\n'
                result += "%s " % (self.language)
                if ctx is not "":
                    result += "(%s) '%s'" % (ctx, source)
                else:
                    result += "'%s'" % (source)
                if translation is None:
                    result += " ! <not translated>"
                else:
                    result += " = '%s'" % (translation)
                if not is_used:
                    result += " ! <not used>"
                if not special_characters_check:
                    result += " ! <special characters mismatch>"
        return result

    def set(self, ctx, source, translation, is_used):
        if ctx not in self.keys:
            self.keys[ctx] = {}
        if source not in self.keys[ctx]:
            self.keys[ctx][source] = collections.namedtuple('Translation', 'translation is_used')
        self.keys[ctx][source].translation = translation
        self.keys[ctx][source].is_used = is_used

    def translation(self, ctx, source):
        if not self.is_exists(ctx, source):
            return None
        return self.keys[ctx][source].translation

    def is_used(self, ctx, source):
        if not self.is_exists(ctx, source):
            return False
        return self.keys[ctx][source].is_used

    def special_characters_check(self, source, translation):
        if source is None or translation is None:
            return True
        r = re.compile(r'[\.\,\:\;\\\/\(\)\[\]\{\}\?\!\&\%\"\'\*\+\~\<\>\|\=\-\r\n\t]')
        count_source = collections.Counter(r.findall(source))
        translation_source = collections.Counter(r.findall(translation))
        return count_source == translation_source

    def is_exists(self, ctx, source):
        if ctx not in self.keys:
            return False
        if source not in self.keys[ctx]:
            return False
        return True

    def ctx_list(self):
        return self.keys.keys()

    def sources_list(self, ctx):
        if ctx not in self.keys:
            return {}
        return self.keys[ctx].keys()

    def merge(self, addition, only_existing):
        for ctx in addition.ctx_list():
            for source in addition.sources_list(ctx):
                translation = addition.translation(ctx, source)
                is_used = addition.is_used(ctx, source)
                exists = self.is_exists(ctx, source)
                if exists:
                    self.set(ctx, source, translation, True)
                else:
                    if not only_existing:
                        self.set(ctx, source, translation, is_used)


def load_sources(language):
    used_keys = Translation(language)
    file = open(PATH + '/../source/Main.cpp', 'r')
    lines = file.readlines()
    for line in lines:
        r = re.compile(r'TR\(\s*"(.*?)"\s*\)')
        pos = 0
        while True:
            m = r.search(line, pos)
            if m is not None:
                pos = m.span()[1]
                ctx = ""
                source = m.group(1)
                used_keys.set(ctx, source, None, True)
            else:
                break
        r = re.compile(r'TR_CTX\(\s*"(.*?)"\s*,\s*"(.*?)"\s*\)')
        pos = 0
        while True:
            m = r.search(line, pos)
            if m is not None:
                pos = m.span()[1]
                ctx = m.group(2)
                source = m.group(1)
                used_keys.set(ctx, source, None, True)
            else:
                break
    return used_keys


def load_translation(language):
    given_keys = Translation(language)
    metadata = None
    file_name = PATH + '/../emuiibo/lng_' + language + '.json'
    try:
        file = open(file_name)
        data = json.load(file)
        metadata = data['metadata']
        for str_entry in data['strings']:
            ctx = str_entry['context']
            source = str_entry['source']
            translation = str_entry['translation']
            given_keys.set(ctx, source, translation, False)
    except:
        print('%s not exists' % (file_name))
    return (given_keys, metadata)


def save_translation(language, result_keys, metadata):
    file_name = PATH + '/../emuiibo/lng_' + language + '.json'
    try:
        data = {}
        if metadata is None:
            now =  datetime.datetime.now()
            data['metadata'] = {}
            data['metadata']['base'] = 'en'
            data['metadata']['language'] = language
            data['metadata']['created'] = "{}-{}-{}".format(now.year, now.month, now.day)
            data['metadata']['author'] = ""
        else:
            data['metadata'] = metadata
        data['strings'] = []
        for ctx in result_keys.ctx_list():
            for source in result_keys.sources_list(ctx):
                translation = result_keys.translation(ctx, source)
                data['strings'].append({
                'context' : ctx,
                'source' : source,
                'translation' : translation})
        file = open(file_name, 'w+')
        json.dump(data, file, indent=4, ensure_ascii=False)
        print('%s was created/updated' % (file_name))
    except:
        print('%s was not created' % (file_name))


def check_language(language):
    ava_lang = {
        "ja": "Japanese",
        "en-US": "AmericanEnglish",
        "fr": "French",
        "de": "German",
        "it": "Italian",
        "es": "Spanish",
        "zh-CN": "Chinese",
        "ko": "Korean",
        "nl": "Dutch",
        "pt": "Portuguese",
        "ru": "Russian",
        "zh-TW": "Taiwanese",
        "en-GB": "BritishEnglish",
        "fr-CA": "CanadianFrench",
        "es-419": "LatinAmericanSpanish",
        "zh-Hans": "[4.0.0+] SimplifiedChinese",
        "zh-Hant": "[4.0.0+] TraditionalChinese",
        "pt-BR": "[10.1.0+] BrazilianPortuguese",
    };
    if language not in ava_lang.keys():
        raise Exception("Language code '%s' is wrong. Allowed: %s" % (language, ava_lang))


def update(parser):
    language = parser.language
    check_language(language)
    remove_unused = parser.remove_unused
    used_keys = load_sources(language)
    (given_keys, metadata) = load_translation(language)
    result_keys = Translation(language)
    result_keys.merge(used_keys, False)
    result_keys.merge(given_keys, remove_unused)
    save_translation(language, result_keys, metadata)
    print(result_keys)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    subparsers = parser.add_subparsers(dest='command', help='Subcommand to run')

    parser_update = subparsers.add_parser('update')
    parser_update.add_argument(
        '--remove-unused', action='store_true', help='Remove unused keys')
    parser_update.add_argument(
        'language', help='Language according https://switchbrew.org/wiki/Settings_services#LanguageCode')
    parser_update.set_defaults(func=update)

    options = parser.parse_args()
    options.func(options)
