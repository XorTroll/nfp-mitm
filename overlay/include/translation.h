#pragma once

#include <string>
#include <unordered_map>

#define TR(SOURCE) Translator::translate(SOURCE)
#define TR_CTX(SOURCE, CONTEXT) Translator::translate(SOURCE, CONTEXT)

class Translator {

public:
    static Translator& global();
    static std::string translate(const std::string& source, const std::string& context = {});

    void loadSystem();

    //https://switchbrew.org/wiki/Settings_services#LanguageCode
    void loadCustom(const std::string& lng);

    std::string get(const std::string& source, const std::string& context) const;

private:
    std::string language;
    std::unordered_map<std::string, std::unordered_map<std::string, std::string>> strings;
};
