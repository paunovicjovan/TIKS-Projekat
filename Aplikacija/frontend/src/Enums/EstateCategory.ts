export enum EstateCategory {
    House = "House",
    Flat = "Flat",
    Office = "Office",
    Retail = "Retail",
}

export const EstateCategoryTranslations: Record<EstateCategory, string> = {
    [EstateCategory.House]: "Kuća",
    [EstateCategory.Flat]: "Stan",
    [EstateCategory.Office]: "Kancelarija",
    [EstateCategory.Retail]: "Maloprodaja",
};

export function getEstateCategoryTranslation(category: EstateCategory): string {
    return EstateCategoryTranslations[category];
}
