"use client";

import { Laptop, Moon, Sun } from "lucide-react";
import { useTranslations } from "next-intl";
import { useTheme } from "next-themes";

import { Button } from "@/components/ui/button";
import { useMounted } from "@/lib/use-mounted";

export function ThemeToggle() {
  const { theme, setTheme } = useTheme();
  const tProfile = useTranslations("settings.profile.themes");
  const tCommon = useTranslations("common");
  const mounted = useMounted();

  if (!mounted) {
    return (
      <Button
        variant="secondary"
        className="h-8 w-8 p-0"
        aria-label={tCommon("toggleTheme")}
        title={tCommon("theme")}
      >
        <Sun className="h-4 w-4 opacity-50" />
      </Button>
    );
  }

  function cycleTheme() {
    if (theme === "light") setTheme("dark");
    else if (theme === "dark") setTheme("system");
    else setTheme("light");
  }

  const themeLabel =
    theme === "dark"
      ? tProfile("dark")
      : theme === "light"
        ? tProfile("light")
        : tProfile("system");

  return (
    <Button
      variant="secondary"
      onClick={cycleTheme}
      className="h-8 w-8 p-0"
      aria-label={`${tCommon("toggleTheme")}: ${themeLabel}`}
      title={`${tCommon("theme")}: ${themeLabel}`}
    >
      {theme === "dark" ? (
        <Moon className="h-4 w-4" />
      ) : theme === "light" ? (
        <Sun className="h-4 w-4" />
      ) : (
        <Laptop className="h-4 w-4" />
      )}
    </Button>
  );
}
