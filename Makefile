
export EMUIIBO_MAJOR := 0
export EMUIIBO_MINOR := 6
export EMUIIBO_MICRO := 3

.PHONY: all clean

all:
	@cd emuiibo; sprinkle nsp --release
	@$(MAKE) -C overlay/
	@rm -rf $(CURDIR)/SdOut
	@mkdir -p $(CURDIR)/SdOut/atmosphere/contents/0100000000000352/flags
	@touch $(CURDIR)/SdOut/atmosphere/contents/0100000000000352/flags/boot2.flag
	@cp $(CURDIR)/emuiibo/target/aarch64-none-elf/release/emuiibo.nsp $(CURDIR)/SdOut/atmosphere/contents/0100000000000352/exefs.nsp
	@cp $(CURDIR)/emuiibo/toolbox.json $(CURDIR)/SdOut/atmosphere/contents/0100000000000352/toolbox.json
	@mkdir -p $(CURDIR)/SdOut/switch/.overlays
	@cp $(CURDIR)/overlay/emuiibo.ovl $(CURDIR)/SdOut/switch/.overlays/emuiibo.ovl
	@cp -r $(CURDIR)/overlay/emuiibo $(CURDIR)/SdOut/switch/.overlays/

clean:
	@rm -rf $(CURDIR)/SdOut
	@cd emuiibo; xargo clean
	@$(MAKE) clean -C overlay/

translation:
	$(CURDIR)/overlay/tools/translation.py update ru $(ARGS)
	$(CURDIR)/overlay/tools/translation.py update de $(ARGS)
